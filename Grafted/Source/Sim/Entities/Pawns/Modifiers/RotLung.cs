namespace Grafted.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class RotLung : BodyPartModifier
{
    private const string ModifierManeuver = "Slingshot";
    private const double DamageFactorPerTick = .011f;

    public override void Tick()
    {
        BodyPart.HitPoints -= BodyPart.HitPoints * DamageFactorPerTick;
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
            Log.Warning($"No lungs found while applying body part modifier {Defs.BodyPartModifiers.RotLung.Moniker}");
            return false;
        }

        lung.TryAddModifier(this);
        
        return true;
    }

    public override Widget? GetInfoPanel() => BuildInfoPanel(new InfoPanelData
    {
        Damage = DamageFactorPerTick * 100,
        DamageSuffix = "% health/tick",
        DamageColor = new Color(120, 100, 80),
        Lines = [new("Targets lungs specifically", new Color(150, 130, 100))],
        BlockedBy = "Plague Mask"
    });
}