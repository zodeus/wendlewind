namespace Wendlemire.Sim.Entities.Items;

public enum DamageType : byte {
    Invalid,
    Sharp,
    Blunt,
    Piercing,
    Flesh,
    Fire,
    Ice,
    Acid,
    Poison,
    Magic
}

public static class DamageTypeExtensions {
    public static bool IsPhysicalDamage(this DamageType damageType) {
        return damageType switch {
            DamageType.Sharp => true,
            DamageType.Blunt => true,
            DamageType.Piercing => true,
            DamageType.Flesh => true,
            _ => false
        };
    }

    public static StatDef? GetResistanceStat(this DamageType damageType) {
        return damageType switch {
            DamageType.Sharp or DamageType.Blunt or DamageType.Piercing or DamageType.Flesh => Defs.Stats.PhysicalResistance,
            DamageType.Fire => Defs.Stats.FireResistance,
            DamageType.Ice => Defs.Stats.IceResistance,
            DamageType.Acid => Defs.Stats.AcidResistance,
            DamageType.Poison => Defs.Stats.PoisonResistance,
            DamageType.Magic => Defs.Stats.MagicResistance,
            _ => null
        };
    }
}