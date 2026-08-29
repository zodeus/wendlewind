namespace Wendlewind.Sim.Combat;

public class DamageRequest
{
    public readonly Pawn Source;
    public Item Weapon { get; }
    public List<Damage> RawDamages = new();
    public WeaponManeuverDef WeaponManeuver { get; }
    public BodyPart TargetedPart { get; set; } = null!;
    public double TotalRawDamage => RawDamages.Sum(damage => damage.TotalDamage);

    public DamageRequest(Pawn source, Item weapon, WeaponManeuverDef weaponManeuver)
    {
        WeaponManeuver = weaponManeuver;
        Source = source;
        Weapon = weapon;
    }

    public static DamageRequest Create(Pawn pawn, Item tool)
    {
        var pawnStrength = pawn.GetStatValue(Defs.Stats.Strength);
        var toolPower = tool.GetStatValue(Defs.Stats.WeaponPower);
        if(tool.ItemDef.WeaponProperties!.WeaponManeuvers.Count == 0)
        {
            throw new Exception("No weapon maneuvers found for weapon: " + tool.ItemDef.Moniker);
        }
        var weaponManeuver = tool.ItemDef.WeaponProperties!.WeaponManeuvers.RandomElement(pawn.Context.Rng);
        var skillPower = 1 + (pawn.GetSkill(tool.ItemDef.WeaponProperties!.WeaponType)?.Level * .1f ?? 0);
        var rawDamage = Mathf.RoundToInt(
            toolPower
            * pawnStrength
            * skillPower
            * weaponManeuver.DamageMultiplier.Roll(pawn.Context.Rng)
        );
        
        var (criticalDamage, isCritical) = CalculateCriticalDamage(pawn, rawDamage);
        rawDamage = criticalDamage;
        if (rawDamage < 0)
        {
            Log.Warning("Damage was negative.");
            rawDamage = 0;
        }

        DamageRequest request = new(pawn, tool, weaponManeuver);
        request.RawDamages.Add(new Damage(tool, rawDamage, weaponManeuver.Label, isCritical));

        return request;
    }

    private static (int damage, bool isCritical) CalculateCriticalDamage(Pawn pawn, int rawDamage)
    {
        //Defs.Stats.CriticalStrikeChance
        if (pawn.Inventory.Contains(Defs.Items.Monocle) && pawn.Context.Rng.Chance(.2f))
        {
            var range = new RangeFloat(1.2f, 2f);
            var critMultiplier = range.Roll(pawn.Context.Rng);
            return (Mathf.RoundToInt(rawDamage * critMultiplier), true);
        }
        return (rawDamage, false);
    }
}