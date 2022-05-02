using System.Collections.Generic;
using Grafted.Sim.Entities.Items;

namespace Grafted.Sim.Entities.Pawns;

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
    public AdaptiveBodyPartProperties? AdaptiveProperties;
}