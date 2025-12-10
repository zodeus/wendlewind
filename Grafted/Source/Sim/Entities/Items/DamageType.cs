namespace Grafted.Sim.Entities.Items;

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
}