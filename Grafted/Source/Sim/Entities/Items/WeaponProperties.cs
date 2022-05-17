using System.Collections.Generic;
using Grafted.Sim.Entities.Pawns;

namespace Grafted.Sim.Entities.Items;

public class WeaponProperties {
    public List<BodyPartModifierRecord> BodyPartModifiers = new();
    public DamageType DamageType = DamageType.Invalid;
}