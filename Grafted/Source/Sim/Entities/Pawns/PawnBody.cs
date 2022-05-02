using System;
using System.Collections.Generic;
using Grafted.Maths;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBody {
    private float _bloodAmount;
    private float _energy = 1;
    private float _ticksWithEmptyStomach;

    public readonly Pawn Pawn;
    public readonly float MaxBlood = 5000;

    public BodyPartSocket RootSocket = null!;
    public float BloodChangeLastFrame;
    public float Temperature = 32;
    public float StomachLevel = 1;
    public PawnCapabilities Capabilities;
    public float BloodPercent => BloodAmount / MaxBlood;

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

    public PawnBody(Pawn pawn) {
        Pawn = pawn;
        BloodAmount = MaxBlood;
        Capabilities = new PawnCapabilities(pawn);
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

    private void CalculateBloodLossForExternalPart(BodyPart part) {
        const float severedArteryBloodLossFactor = 4f;
        const float severedLimbBloodLossFactor = 6f;
        float bloodLossScaleFactor = part.Size / 6;

        if (part.HealthPercent < .95) {
            //Log.Info($"{_pawn} {part} losing {bloodLossScaleFactor * (1 - part.HealthPercent)}");
            BloodAmount -= bloodLossScaleFactor * (1 - part.HealthPercent);
        }

        // stop part traversal if part is an artery and it's been severed
        bool continuePartTraversal = true;
        foreach (BodyPart internalPart in part.InternalParts) {
            if (internalPart.Type != BodyPartType.Artery || internalPart.HealthPercent >= 1) {
                continue;
            }

            if (internalPart.IsDestroyed) {
                //Log.Info($"{_pawn} {internalPart} losing {bloodLossScaleFactor * severedArteryBloodLossFactor}");
                BloodAmount -= Math.Max(bloodLossScaleFactor * severedArteryBloodLossFactor, 10f);
                // Artery is severed stop propagating bleeding
                continuePartTraversal = true;
                continue;
            }

            //Log.Info($"{_pawn} {internalPart} losing {bloodLossScaleFactor * (1.3f - part.HealthPercent)}");
            BloodAmount -= bloodLossScaleFactor * (1.3f - part.HealthPercent);
        }

        foreach (BodyPartSocket socket in part.Sockets) {
            if (socket.AttachedPart == null) {
                // part has been severed, start hemorrhaging
                if (socket.IsSealed == false) {
                    //Log.Info($"{_pawn} {socket} losing {bloodLossScaleFactor * severedLimbBloodLossFactor}");
                    BloodAmount -= Math.Max(bloodLossScaleFactor * severedLimbBloodLossFactor, 15);
                }

                continue;
            }

            if (continuePartTraversal && socket.AttachedPart?.IsExternal == true) {
                CalculateBloodLossForExternalPart(socket.AttachedPart);
            }
        }
    }

    public void Tick() {
        // Heat Calculations
        PushExternalHeat();

        // Stomach Calculations
        StomachLevel = Mathf.Clamp(StomachLevel - 0.002f, 0, 1);
        if (StomachLevel <= 0) {
            _ticksWithEmptyStomach++;
        }
        else {
            _ticksWithEmptyStomach = 0;
        }

        // Malnutrition Calculations
        if (_ticksWithEmptyStomach > SimTime.HoursToTicks(24)) {
            TakeMalnutritionDamage();
        }

        // Energy Calculations
        ApplyEnergyLossFactor(0.0004f);

        // Blood Loss Calculations
        if (RootSocket.AttachedPart == null) {
            BloodAmount = 0;
            BloodChangeLastFrame = 0;
        }
        else {
            float preTickBloodAmount = BloodAmount;
            float preTickBloodPercent = BloodPercent;
            CalculateBloodLossForExternalPart(RootSocket.AttachedPart!);
            foreach (BodyPart bodyPart in AllParts) {
                bodyPart.TicksSinceLastHit++;
            }

            Regenerate();
            if (Math.Abs(preTickBloodPercent - BloodPercent) > .00001) {
                BloodChangeLastFrame = BloodPercent - preTickBloodPercent;
            }
            else {
                BloodAmount = preTickBloodAmount;
                BloodChangeLastFrame = 0;
            }
        }

        if (BloodAmount <= 1) {
            Pawn.IsDead = true;
            if (Pawn.PawnType == PawnType.Player) {
                Core.Sim.Messages.Push(new Message($"\\c[{UiTextColor.TextColorPawn}]{Pawn.Label} \\c[{UiTextColor.TextColorRed}]died from blood loss"));
            }

            Core.Sim.World.DeathRecords.RecordDeath(new DeathRecord {
                Round = Core.Sim.World.TotalKills + 1,
                PawnName = Pawn.Label,
                CauseOfDeath = $"Blood Loss"
            });
        }
    }

    public void ApplyEnergyLossFactor(float factor) {
        Energy -= _ticksWithEmptyStomach > 0 ? Energy * factor * 2 : Energy * factor;
    }

    private void TakeMalnutritionDamage() {
        if (Core.Sim.World.Time.IsIntervalOf(SimTime.MinutesToSeconds(10)) == false) {
            return;
        }

        foreach (BodyPart bodyPart in AllParts) {
            if (bodyPart.Type == BodyPartType.Artery) {
                continue;
            }

            if (Core.Random.Chance(0.7f)) {
                continue;
            }

            bodyPart.HitPoints -= bodyPart.HitPoints * Core.Random.NextFloat(0.0001f, 0.0005f);
        }
    }

    private void PushExternalHeat() {
        float temp = Pawn.Zone?.Temperature ?? 0;
        if (Pawn.Zone?.Town?.GetStructure<TownStructureHouse>() is { IsFireBurning: true }) {
            temp = 22;
        }

        if (temp > 40) {
            Temperature = Mathf.Clamp(Temperature + 1, Temperature, temp + 10);
        }
        else if (temp is >= 18 and <= 40) {
            if (Temperature > 32) {
                Temperature = Mathf.Clamp(Temperature - 1, 32, Temperature);
            }
            else {
                if (Temperature < 32) {
                    Temperature = Mathf.Clamp(Temperature + 1, Temperature, 32);
                }
            }
        }
        else if (temp < 18) {
            Temperature = Mathf.Clamp(Temperature - 1, temp + 10, Temperature);
        }
    }

    private void Regenerate() {
        if (StomachLevel <= 0) {
            return;
        }

        if (RootSocket.AttachedPart == null) {
            return;
        }

        // stop regenerating blood when near death
        if (BloodAmount > 100) {
            BloodAmount += 1f;
        }

        const float regenerationFactor = 0.001f;

        void UpdateHealth(BodyPart bodyPart) {
            if (bodyPart.IsDestroyed) {
                return;
            }

            bodyPart.HitPoints += bodyPart.HitPoints * regenerationFactor;
        }

        void DoRegeneration(BodyPart bodyPart) {
            UpdateHealth(bodyPart);
            foreach (BodyPart internalPart in bodyPart.InternalParts) {
                UpdateHealth(internalPart);
            }

            foreach (BodyPart externalPart in bodyPart.ExternalParts) {
                DoRegeneration(externalPart);
            }
        }

        DoRegeneration(RootSocket.AttachedPart);
    }
}