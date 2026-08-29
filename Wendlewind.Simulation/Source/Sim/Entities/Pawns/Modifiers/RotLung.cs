namespace Wendlewind.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class RotLung : BodyPartModifier
{
    private const string ModifierManeuver = "Slingshot";
    private const double DamagePerTick = .1;

    public override void Tick()
    {
        BodyPart.HitPoints -= DamagePerTick;
        CheckIfLostVitalPart();
        base.Tick();
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.Body?.Pawn.Equipment.Armor.Any(i => i.ItemDef == Defs.Items.PlagueMask) == true)
        {
            return false;
        }

        if (part.Type is not (BodyPartType.Head or BodyPartType.Neck or BodyPartType.Torso) && Maneuver != ModifierManeuver)
        {
            return false;
        }

        var lung = part.Type == BodyPartType.Lung ? part : part.Body?.AllParts.Where(p => p?.Type == BodyPartType.Lung).RandomElement();
        if (lung == null)
        {
            return false;
        }

        lung.TryAddModifier(this);
        
        return true;
    }

    public override InfoPanelData GetInfoData() => new InfoPanelData
    {
        Damage = DamagePerTick,
        DamageSuffix = "health/tick",
        DamageColor = new Color(120, 100, 80),
        Lines =
        [
            new("Spreads to lungs from head/neck/torso", new Color(150, 130, 100)),
            new("Can cause death from lung failure", InfoColors.Damage)
        ],
        BlockedBy = "Plague Mask"
    };
}