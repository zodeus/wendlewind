using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities.Pawns;

public class PawnBody {
    public readonly Pawn Pawn;
    public BodyPartSocket RootSocket = null!;
    public readonly float MaxBlood = 5000;
    public float BloodLevel => BloodAmount / MaxBlood;
    public float BloodAmount;

    public List<BodyPart> AllParts {
        get {
            List<BodyPart> parts = new();
            GetParts(RootSocket.AttachedPart!, parts);
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
        const float severedArteryBloodLossFactor = 1.3f;
        const float severedLimbBloodLossFactor = 4f;
        float bloodLossScaleFactor = part.Size / 3;

        if (part.HealthPercent < .95) {
            //Log.Info($"{_pawn} {part} losing {bloodLossScaleFactor * (1 - part.HealthPercent)}");
            BloodAmount -= bloodLossScaleFactor * (1 - part.HealthPercent);
        }

        bool continuePartTraversal = true;
        foreach (BodyPart internalPart in part.InternalParts) {
            if (internalPart.Type != BodyPartType.Artery || internalPart.HealthPercent >= 1) {
                continue;
            }

            if (part.IsDestroyed) {
                //Log.Info($"{_pawn} {internalPart} losing {bloodLossScaleFactor * severedArteryBloodLossFactor}");
                BloodAmount -= bloodLossScaleFactor * severedArteryBloodLossFactor;
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
                    BloodAmount -= bloodLossScaleFactor * severedLimbBloodLossFactor;
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
        }
        else {
            CalculateBloodLossForExternalPart(RootSocket.AttachedPart!);
            foreach (BodyPart bodyPart in AllParts) {
                bodyPart.TicksSinceLastHit++;
            }
        }

        if (BloodAmount <= 0) {
            Pawn.IsDead = true;
            Core.Sim.World.DeathRecords.RecordDeath(new DeathRecord {
                Round = Core.Sim.World.TotalKills + 1,
                PawnName = Pawn.Label,
                CauseOfDeath = $"Blood Loss"
            });
        }
    }
}

public class BodyPartDef : EntityDef {
    public override EntityType EntityType => EntityType.BodyPart;
    //public override Type DefUiClass => typeof(ItemDefPanel);
    public BodyPartType BodyPartType = BodyPartType.Undefined;
    public float Size = 0;
    public float HitWeight = 0;
    public bool IsVital = false;
    public bool IsOrgan = false;
    public bool IsFlesh = false;
    public bool IsBone = false;
    public List<BodyPartSocketDef> Sockets = new();
    public List<EquipmentSlotType>? EquipmentSlots = null;
}

public class BodyPartSocketDef : Def {
    public bool IsExternal = false;
    public List<BodyPartType> AllowedBodyPartTypes = new();
}

public enum BodyPartType {
    Undefined,
    Head,
    Artery,
    Bone,
    Brain,
    Eye,
    Neck,
    Torso,
    Arm,
    Hand,
    Finger,
    Thumb,
    Leg,
    Foot,
    Toe,
    Skin,
    Skull,
    RibCage,
    Stomach,
    Heart,
    Lung
}