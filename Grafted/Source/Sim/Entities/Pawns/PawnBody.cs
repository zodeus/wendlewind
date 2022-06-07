using System;
using System.Collections.Generic;
using System.Linq;
using Grafted.Definitions;
using Grafted.Maths;
using Grafted.Sim.Entities.Pawns.Bodies.Handlers;
using Grafted.Sim.Persistence;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBody : IExposable, IIdentityProvider {
    private float _bloodAmount;
    private float _energy = 1;
    private int _sequencePoints = 0;
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

    public bool IsExhausted => TicksSinceLastRest > SimTime.TicksPerDay;

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

    public void Tick() {
        foreach (BodyPart bodyPart in AllParts) {
            bodyPart.Tick();
        }

        Effects.Tick();

        TicksSinceLastRest++;
        if (Pawn.IsResting) {
            TicksSinceLastRest = 0;
        }

        Handler.Tick();

        if (BloodAmount <= 1) {
            HandleBloodLossDeath();
        }
    }

    public void PushExternalHeat() {
        Handler.PushExternalHeat();
    }

    public void ConsumeEnergy(float baseAmount) {
        Handler.ConsumeEnergy(baseAmount);
    }

    public int GetSequencePoints() {
        if (BodyPartsDirty) {
            _sequencePoints = Mathf.RoundToInt(RootSocket.AttachedPart?.SequencePoints ?? 0);

            //todo this calculates lung capacity, I think there should be a capacities list object somewhere instead, perhaps PawnBody or on Pawn? Need to add events to BodyPart to properly implement
            _sequencePoints = Mathf.RoundToInt(_sequencePoints * Capabilities.Breathing);
            BodyPartsDirty = false;
        }

        if (Energy < .50) {
            return _sequencePoints - 1;
        }

        if (Energy < .25) {
            return _sequencePoints - 2;
        }

        return _sequencePoints;
    }

    public string GetUniqueId() {
        return Id;
    }

    public void ExposeData() {
        Scribe_Values.Look(ref Id!, "Id");
        Scribe_Values.Look(ref _bloodAmount, "BloodAmount");
        Scribe_Values.Look(ref _energy, "Energy");
        Scribe_Values.Look(ref BloodChangeLastFrame, "BloodChangeLastFrame");
        Scribe_Values.Look(ref Temperature, "Temperature");
        Scribe_Values.Look(ref StomachLevel, "StomachLevel");
        Scribe_Deep.Look(ref Capabilities!, "Capabilities", Pawn);
        Scribe_Deep.Look(ref Effects!, "Effects", Pawn);
        Scribe_Deep.Look(ref _rootSocket!, "RootSocket");
        Scribe_Deep.Look(ref Handler!, "Handler");
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

    private void HandleBloodLossDeath() {
        Pawn.IsDead = true;
        if (Pawn.PawnType == PawnType.Player) {
            Core.Sim.Messages.Push(new Message($"\\c[{UiTextColor.TextColorPawn}]{Pawn.Label} \\c[{UiTextColor.TextColorRed}]died from blood loss"));
        }

        Core.Sim.World.DeathRecords.RecordDeath(new DeathRecord {
            Round = Core.Sim.World.TotalKills + 1,
            PawnName = Pawn.Label,
            CauseOfDeath = "Blood Loss"
        });
    }
}