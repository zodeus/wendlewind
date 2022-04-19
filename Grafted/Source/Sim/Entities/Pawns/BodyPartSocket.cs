using Grafted.Definitions;
using Grafted.Maths;

namespace Grafted.Sim.Entities.Pawns;

public class BodyPartSocket {
    public BodyPartSocketDef Def;
    public BodyPart? AttachedPart;
    public BodyPart? ParentPart;
    public bool IsSealed = false;

    public bool IsExternal => Def.IsExternal;

    public string Label => Def.Label;

    public BodyPartSocket(BodyPartSocketDef def, BodyPart? parentPart = null) {
        Def = def;
        ParentPart = parentPart;
    }

    public BodyPart TryAttachPart(BodyPartDef def) {
        return TryAttachPart(EntityGenerator.CreateEntity<BodyPart>(def));
    }

    public BodyPart TryAttachPart(BodyPart bodyPart) {
        if (CanSocket(bodyPart.Type) == false) {
            throw new System.NotImplementedException();
        }

        AttachedPart = bodyPart;
        bodyPart.Socket = this;
        IsSealed = true;

        //todo not sure if this should happen here...
        if (bodyPart.Type is BodyPartType.Skin) {
            bodyPart.MaxHitPoints = Mathf.FloorToInt(.7f * ParentPart!.GetStatValue(Defs.Stats.MaxHitPoints));
            bodyPart.HitPoints = bodyPart.MaxHitPoints;
        }

        //todo not sure if this should happen here...
        if (bodyPart.Type is BodyPartType.Bone) {
            bodyPart.MaxHitPoints = Mathf.FloorToInt(.85f * ParentPart!.GetStatValue(Defs.Stats.MaxHitPoints));
            bodyPart.HitPoints = bodyPart.MaxHitPoints;
        }

        if (bodyPart.Type is BodyPartType.Artery) {
            bodyPart.MaxHitPoints = bodyPart.Socket.ParentPart!.Size switch {
                < 10 => 5,
                < 30 => 7,
                < 80 => 10,
                _ => 15
            };
            bodyPart.HitPoints = bodyPart.MaxHitPoints;
        }

        return bodyPart;
    }

    public bool CanSocket(BodyPartType bodyPartType) {
        return Def.AllowedBodyPartTypes.Contains(bodyPartType);
    }

    public override string ToString() {
        return Def.Moniker;
    }
}