using System.Diagnostics.Eventing.Reader;
using Grafted.Sim.Entities.Pawns.Bodies.Handlers;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBody : IExposable, IIdentityProvider
{
    private float _bloodAmount;
    private float _baseAttackSpeed;
    private BodyPartSocket _rootSocket = null!;

    public readonly Pawn Pawn;
    public string Id = "invalid";
    public float BodySizeFactor = 1;
    public float Energy = 1;
    public float Temperature = 32;
    public float StomachLevel = 1;
    public PawnCapabilities Capabilities = null!;
    public PawnBodyEffects Effects = null!;
    public bool RequiresLungs = true;
    public bool BodyPartsDirty = true;
    public float BloodChangeLastFrame;
    public BodyStanceDef Stance = null!;
    public DefaultBodyHandler Handler = null!;
    public BodyDef Def => Pawn.PawnDef.Body;
    public float MaxBlood => Def.MaxBlood * Pawn.Body.BodySizeFactor;
    public float MaxEnergy => Def.MaxEnergy;
    public bool IsFamished => Handler.IsFamished;

    public BodyPartSocket RootSocket
    {
        get => _rootSocket;
        set
        {
            _rootSocket = value;
            _rootSocket.Body = this;
        }
    }

    public float BloodPercent => BloodAmount / MaxBlood;
    public bool IsWarm => Temperature is > 10 and < 40;

    public float MovementSpeed
    {
        get
        {
            var moveBonus = AllExternalParts.SelectMany(p => p.Equipment.Values).Sum(v => v?.GetStatValue(Defs.Stats.MoveSpeed) ?? 0);
            var capacityFactor = Capabilities.Mobility * Capabilities.Breathing * Math.Max(Capabilities.Sight, .8f);
            return (Pawn.GetStatValue(Defs.Stats.MoveSpeed) + moveBonus) * capacityFactor;
        }
    }

    public float BloodAmount
    {
        get => _bloodAmount;
        set => _bloodAmount = Mathf.Clamp(value, 0f, MaxBlood);
    }

    public float EnergyPercent => Energy / MaxEnergy;

    public List<BodyPart> AllParts
    {
        get
        {
            List<BodyPart> parts = new();
            if (RootSocket.AttachedPart != null)
            {
                GetParts(RootSocket.AttachedPart!, parts);
            }

            return parts;
        }
    }

    public List<BodyPart> AllExternalParts
    {
        get
        {
            List<BodyPart> parts = new();
            if (RootSocket.AttachedPart != null)
            {
                GetParts(RootSocket.AttachedPart, parts, true);
            }

            return parts;
        }
    }

    public bool IsHungry => Handler.IsHungry;

    public PawnBody(Pawn pawn)
    {
        Pawn = pawn;
    }

    public void Initialize()
    {
        Id = $"{Pawn.Id}-Body";
        Capabilities = new PawnCapabilities(Pawn);
        Effects = new PawnBodyEffects(Pawn);
        Handler = Def.Handler;
        Handler.Initialize(this);
        Energy = MaxEnergy;
        Stance = Defs.BodyStances.Comfortable;
    }

    public void Tick()
    {
        foreach (BodyPart bodyPart in AllParts)
        {
            bodyPart.Tick();
            if (Pawn.IsDead)
            {
                return;
            }
        }

        Effects.Tick();

        Handler.Tick();

        if (Def.BloodType != null && BloodAmount <= 1)
        {
            Pawn.TriggerDeath("Blood loss");
        }
    }

    public void ConsumeEnergy(float amount)
    {
        if (Effects.Has(Defs.BodyEffects.Fruiting))
        {
            amount *= 0.5f;
        }

        Handler.ConsumeEnergy(amount);
    }

    public float GetAttackSpeedModifier()
    {
        if (BodyPartsDirty)
        {
            _baseAttackSpeed = RootSocket.AttachedPart?.AttackSpeedModifier ?? 0;
            _baseAttackSpeed *= Capabilities.Breathing;
            BodyPartsDirty = false;
        }

        if (EnergyPercent < .90f)
        {
            return Mathf.Clamp(_baseAttackSpeed - ((1 - (_baseAttackSpeed * EnergyPercent)) / 2), 0, 100);
        }

        return _baseAttackSpeed;
    }

    public string GetUniqueId()
    {
        return Id;
    }

    public void ExposeData()
    {
        ScribeValues.Look(ref Id!, "Id");
        ScribeValues.Look(ref _bloodAmount, "BloodAmount");
        ScribeValues.Look(ref Energy, "Energy");
        ScribeValues.Look(ref BloodChangeLastFrame, "BloodChangeLastFrame");
        ScribeValues.Look(ref Temperature, "Temperature");
        ScribeValues.Look(ref StomachLevel, "StomachLevel");
        ScribeDeep.Look(ref Capabilities!, "Capabilities", Pawn);
        ScribeDeep.Look(ref Effects!, "Effects", Pawn);
        ScribeDefs.Look(ref Stance!, "Stance");
        ScribeDeep.Look(ref _rootSocket!, "RootSocket");
        ScribeDeep.Look(ref Handler!, "Handler");
    }

    private void GetParts(BodyPart part, List<BodyPart> parts, bool externalOnly = false)
    {
        if (externalOnly == false || (externalOnly && part.IsExternal))
        {
            parts.Add(part);
        }

        foreach (BodyPartSocket socket in part.Sockets)
        {
            if (socket.AttachedPart != null)
            {
                GetParts(socket.AttachedPart, parts, externalOnly);
            }
        }
    }

    public void ModifyStat(StatDef stat, ref float value)
    {
        ModifyStatByStance(stat, ref value);
    }

    private void ModifyStatByStance(StatDef stat, ref float value)
    {
        if (Stance.AffectedStats == null)
        {
            return;
        }

        foreach (var affectedStat in Stance.AffectedStats)
        {
            if (affectedStat.Stat != stat)
            {
                continue;
            }

            if (affectedStat.Factor != null)
            {
                value += (value * affectedStat.Factor.Value);
            }

            if (affectedStat.Offset != null)
            {
                value += affectedStat.Offset.Value;
            }

            value *= 1 + ((Pawn.GetSkill(Stance)?.Level ?? 1) - 1) * 0.01f;
            return;
        }
    }
}