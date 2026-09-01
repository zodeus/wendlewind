using Wendlemire.Sim.Entities.Pawns.Bodies.Handlers;

namespace Wendlemire.Sim.Entities.Pawns;

public class PawnBody : IExposable, IIdentityProvider
{
    private float _bloodAmount;
    private float _baseAttackSpeed;
    private BodyPartSocket _rootSocket = null!;
    private List<BodyPart>? _allPartsCache;
    private List<BodyPart>? _allExternalPartsCache;

    public readonly Pawn Pawn;
    public event Action<BodyPart, double>? TickHealthChanged;
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
    public float MaxBloodBonus { get; set; }
    public float MaxBlood => (Def.MaxBlood + MaxBloodBonus) * Pawn.Body.BodySizeFactor;
    public float MaxEnergy => Def.MaxEnergy;
    public bool IsFamished => Handler.IsFamished;

    public BodyPartSocket RootSocket
    {
        get => _rootSocket;
        set
        {
            _rootSocket = value;
            _rootSocket.Body = this;
            InvalidatePartCaches();
        }
    }

    public float BloodPercent => BloodAmount / MaxBlood;
    public bool IsWarm => Temperature is > 10 and < 40;

    public bool IsSelfPartsDamaged(float fraction, float healthThreshold)
    {
        var externalParts = AllExternalParts;
        var eyes = externalParts.Where(p => p.Type == BodyPartType.Eye).ToList();
        if (eyes.Count > 0 && eyes.All(e => !e.IsFunctional))
        {
            return true;
        }

        var threshold = healthThreshold > 0 ? healthThreshold : 0.6f;
        var damagedCount = externalParts.Count(p => p.HealthPercent < threshold);
        return damagedCount >= externalParts.Count * fraction;
    }

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

    public double HitPoints => AllParts.Sum(p => p.HitPoints);
    public double MaxHitPoints => AllParts.Sum(p => p.MaxHitPoints);

    public List<BodyPart> AllParts
    {
        get
        {
            if (_allPartsCache != null)
            {
                return _allPartsCache;
            }

            _allPartsCache = new List<BodyPart>();
            if (RootSocket.AttachedPart != null)
            {
                GetParts(RootSocket.AttachedPart, _allPartsCache);
            }

            return _allPartsCache;
        }
    }

    public List<BodyPart> AllExternalParts
    {
        get
        {
            if (_allExternalPartsCache != null)
            {
                return _allExternalPartsCache;
            }

            _allExternalPartsCache = new List<BodyPart>();
            if (RootSocket.AttachedPart != null)
            {
                GetParts(RootSocket.AttachedPart, _allExternalPartsCache, true);
            }

            return _allExternalPartsCache;
        }
    }

    public void InvalidatePartCaches()
    {
        _allPartsCache = null;
        _allExternalPartsCache = null;
        BodyPartsDirty = true;
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
        Handler = Def.CreateHandler(Pawn.Context.Factory);
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
            Pawn.TriggerDeath(new DeathRecord
            {
                CauseOfDeath = "Blood loss",
                KillingWeapon = "Blood loss",
                KillingManeuver = "Blood loss"
            });
        }
    }

    public void NotifyTickHealthChanged(BodyPart part, double delta)
    {
        TickHealthChanged?.Invoke(part, delta);
    }

    public BodyPart? FindPartByKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        foreach (var part in AllParts)
        {
            if (part.InternalLabel == key)
            {
                return part;
            }
        }

        return null;
    }

    public void ConsumeEnergyFromAttack()
    {
        float amount = 0.25f; //todo Move somewhere cool, you know... do something with this. Make it dynamic.
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
             //var value = _baseAttackSpeed - ( _baseAttackSpeed / 2 * EnergyPercent);
            var value = _baseAttackSpeed - ( _baseAttackSpeed / 8   * EnergyPercent);
            return Mathf.Clamp(value, 0, 100);
            
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
        InvalidatePartCaches();
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
        Handler.ModifyStat(stat, ref value);
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
                value += value * affectedStat.Factor.Value;
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