using Grafted.Sim.Entities.Pawns.Bodies.Handlers;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBody : IExposable, IIdentityProvider {
    private float _bloodAmount;
    private float _energy = 1;
    private float _attackSpeedModifier = 0;
    private BodyPartSocket _rootSocket;

    public readonly Pawn Pawn;
    public string Id = "invalid";
    public float BodySizeFactor = 1;
    public float Temperature = 32;
    public float StomachLevel = 1;
    public PawnCapabilities Capabilities;
    public PawnBodyEffects Effects;
    public bool BodyPartsDirty = true;
    public float BloodChangeLastFrame;
    public int TicksSinceLastRest = 0;
    public DefaultBodyHandler Handler = null!;
    public BodyDef Def => Pawn.PawnDef.Body;
    public float MaxBlood => Def.MaxBlood * Pawn.Body.BodySizeFactor;
    public bool IsFamished => Handler.IsFamished;

    public BodyPartSocket RootSocket {
        get => _rootSocket;
        set {
            _rootSocket = value;
            _rootSocket.Body = this;
        }
    }

    public float BloodPercent => BloodAmount / MaxBlood;
    public bool IsWarm => Temperature is > 10 and < 40;

    public float MovementSpeed {
        get {
            float moveBonus = AllExternalParts.SelectMany(p => p.Equipment.Values).Sum(v => v?.GetStatValue(Defs.Stats.MoveSpeed) ?? 0);
            return 1 + moveBonus;
        }
    }

    public float BloodAmount {
        get => _bloodAmount;
        set => _bloodAmount = Mathf.Clamp(value, 0f, MaxBlood);
    }

    public float Energy {
        get => _energy;
        set => _energy = Mathf.Clamp(value, 0f, 1);
    }

    public List<BodyPart> AllParts {
        get {
            List<BodyPart> parts = new();
            if (RootSocket.AttachedPart != null) {
                GetParts(RootSocket.AttachedPart!, parts);
            }

            return parts;
        }
    }

    public List<BodyPart> AllExternalParts {
        get {
            List<BodyPart> parts = new();
            if (RootSocket.AttachedPart != null) {
                GetParts(RootSocket.AttachedPart, parts, true);
            }

            return parts;
        }
    }

    public bool IsHungry => Handler.IsHungry;

    public PawnBody(Pawn pawn) {
        Pawn = pawn;
    }

    public void Initialize() {
        Id = $"{Pawn.Id}-Body";
        BloodAmount = Pawn.PawnDef.Body.MaxBlood;
        Capabilities = new PawnCapabilities(Pawn);
        Effects = new PawnBodyEffects(Pawn);
        Handler = Def.Handler;
        Handler.Initialize(this);
    }

    public void Tick(int ticks) {
        foreach (BodyPart bodyPart in AllParts) {
            bodyPart.Tick(ticks);
        }

        Effects.Tick();

        TicksSinceLastRest++;
        if (Pawn.IsResting) {
            TicksSinceLastRest = 0;
        }

        Handler.Tick();

        if (BloodAmount <= 1) {
            Pawn.HandleDeath("Blood loss");
        }
    }

    public void ConsumeEnergy(float amount) {
        Handler.ConsumeEnergy(amount);
    }

    public float GetAttackSpeedModifier() {
        if (BodyPartsDirty) {
            _attackSpeedModifier = RootSocket.AttachedPart?.AttackSpeedModifier ?? 0;
            _attackSpeedModifier *= Capabilities.Breathing;
            BodyPartsDirty = false;
        }

        if (Energy < .25) {
            return _attackSpeedModifier - (_attackSpeedModifier * .4f);
        }
        
        if (Energy < .50) {
            return _attackSpeedModifier - (_attackSpeedModifier * .2f);
        }

        return _attackSpeedModifier;
    }

    public string GetUniqueId() {
        return Id;
    }

    public void ExposeData() {
        ScribeValues.Look(ref Id!, "Id");
        ScribeValues.Look(ref _bloodAmount, "BloodAmount");
        ScribeValues.Look(ref _energy, "Energy");
        ScribeValues.Look(ref BloodChangeLastFrame, "BloodChangeLastFrame");
        ScribeValues.Look(ref Temperature, "Temperature");
        ScribeValues.Look(ref StomachLevel, "StomachLevel");
        ScribeDeep.Look(ref Capabilities!, "Capabilities", Pawn);
        ScribeDeep.Look(ref Effects!, "Effects", Pawn);
        ScribeDeep.Look(ref _rootSocket!, "RootSocket");
        ScribeDeep.Look(ref Handler!, "Handler");
    }

    private void GetParts(BodyPart part, List<BodyPart> parts, bool externalOnly = false) {
        if (externalOnly == false || (externalOnly && part.IsExternal)) {
            parts.Add(part);
        }

        foreach (BodyPartSocket socket in part.Sockets) {
            if (socket.AttachedPart != null) {
                GetParts(socket.AttachedPart, parts, externalOnly);
            }
        }
    }
}