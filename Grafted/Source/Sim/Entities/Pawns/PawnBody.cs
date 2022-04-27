using System;
using System.Collections.Generic;
using Grafted.Maths;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBody {
    public readonly Pawn Pawn;
    public BodyPartSocket RootSocket = null!;
    public readonly float MaxBlood = 5000;
    public float BloodPercent => BloodAmount / MaxBlood;
    public float _bloodAmount;
    public float BloodChangeLastFrame = 0;

    public float BloodAmount {
        get => _bloodAmount;
        set => _bloodAmount = Mathf.Clamp(value, 0f, MaxBlood);
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


    private void Regenerate() {
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