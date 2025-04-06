using Grafted.Sim.Entities;

namespace Grafted.Sim.Combat;

public class DamageRequest
{
    public readonly Pawn Source;
    public Item Weapon { get; }
    public List<Damage> RawDamages = new(1);
    public List<Item> Trinkets { get;  set; } = [];
    public WeaponManeuverDef WeaponManeuver { get; }
    public BodyPart? TargetedPart { get; set; }

    public double TotalRawDamage => RawDamages.Sum(damage => damage.TotalDamage);

    public DamageRequest(Pawn source, Item weapon, WeaponManeuverDef weaponManeuver)
    {
        WeaponManeuver = weaponManeuver;
        Source = source;
        Weapon = weapon;
    }

    public static DamageRequest Create(Pawn pawn, Item tool)
    {
        var pawnStrength = pawn.GetStatValue(Defs.Stats.MeleeStrength);
        var toolPower = tool.GetStatValue(Defs.Stats.MeleePower);
        var weaponManeuver = tool.ItemDef.WeaponProperties!.WeaponManeuvers.RandomElement();
        var skillPower = 1 + (pawn.GetSkill(tool.ItemDef.WeaponProperties!.WeaponType)?.Level * .1f ?? 0);
        var rawDamage = Mathf.RoundToInt(
            toolPower
            * pawnStrength
            * skillPower
            * weaponManeuver.DamageMultiplier.RandomValue
        );
        //rawDamage *= tool.GetStatValue(Defs.Stats.WeaponDamageMultiplier);
        if (rawDamage < 0)
        {
            Log.Warning("Damage was negative.");
            rawDamage = 0;
        }

        DamageRequest request = new(pawn, tool, weaponManeuver);
        request.RawDamages.Add(new Damage(tool, rawDamage, weaponManeuver.Label));

        return request;
    }
}