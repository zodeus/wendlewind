namespace Wendlewind.Sim.Entities.Pawns.Modifiers;

[UsedImplicitly]
public class PoppingPustulesHandler : BodyPartModifier
{
    // Damage dealt each tick to the affected part
    private static RangeDouble SkinDamagePerTick = new(1,1);
    private static RangeDouble FleshDamagePerTick = new(2,2);

    // Pop damage - dealt to surrounding parts when the pustules burst
    private static RangeDouble PopDamageToSurrounding = new(5, 20);
    private static RangeDouble PopDamageToSelf = new(10, 30);
    private static RangeDouble PopDamageToSelfArteries = new(3, 7);
    private static RangeDouble PopDamageToSelfOrgans = new(4, 12);

    // Spread chance when popping (0.0 to 1.0)
    private const double SpreadChance = 0.7;
    private static RangeInt SpreadDuration = new(30, 120);

    public override List<SubstanceType> AllowedSubstances => [SubstanceType.Flesh, SubstanceType.Chitin];

    public override void Tick()
    {
        if (BodyPart.IsSevered)
        {
            return;
        }
        var damage = BodyPart.Type == BodyPartType.Skin ? SkinDamagePerTick : FleshDamagePerTick;
        BodyPart.HitPoints -= damage.RandomValue;

        CheckIfLostVitalPart();
        base.Tick();

        // If we just expired, trigger the pop
        if (IsExpired)
        {
            Pop();
        }
    }

    private void Pop()
    {
        var body = BodyPart.Body;
        if (body == null) return;

        // Deal heavy damage to the part the pustule was on
        BodyPart.HitPoints -= PopDamageToSelf.RandomValue;

        // Damage and potentially spread to surrounding parts
        DamageAndSpreadToSurroundingParts(BodyPart);

        CheckIfLostVitalPart();
    }

    private double GetSelfDamageAmount(BodyPart part)
    {
        if (part.Type == BodyPartType.Artery)
        {
            return PopDamageToSelfArteries.RandomValue;
        }
        else if (part.IsOrgan)
        {
            return PopDamageToSelfOrgans.RandomValue;
        }
        return PopDamageToSelf.RandomValue;
    }
    
    private double GetSurroundingDamageAmount(BodyPart part)
    {
        return PopDamageToSurrounding.RandomValue;
    }

    private void DamageAndSpreadToSurroundingParts(BodyPart bodyPart)
    {
        var rootPart = bodyPart;
        if (bodyPart.Type == BodyPartType.Skin)
        {
            rootPart = bodyPart.Socket!.ParentPart!;
            rootPart.HitPoints -= GetSelfDamageAmount(rootPart);
        }

        // Damage internal parts (flesh, organs, etc.)
        foreach (var internalPart in rootPart.InternalParts)
        {
            internalPart.HitPoints -= GetSelfDamageAmount(internalPart);
        }

        // Damage and potentially spread to parent part
        if (rootPart.Socket?.ParentPart is { } parentPart)
        {
            parentPart.HitPoints -= GetSurroundingDamageAmount(parentPart);
            if (parentPart.Skin is { } skin)
            {
                skin.HitPoints -= GetSurroundingDamageAmount(skin);
            }
            TrySpreadTo(parentPart);
        }

        // Damage and potentially spread to child parts (connected limbs)
        foreach (var externalPart in rootPart.ExternalParts)
        {
            externalPart.HitPoints -= GetSurroundingDamageAmount(externalPart);
            if (externalPart.Skin is { } skin)
            {
                skin.HitPoints -= GetSurroundingDamageAmount(skin);
            }
            TrySpreadTo(externalPart);
        }
    }

    private void TrySpreadTo(BodyPart targetPart)
    {
        if (Core.Random.Chance((float)SpreadChance) == false) return;
        if (targetPart.HasModifier(Def)) return;
        if (AllowedSubstances.Contains(targetPart.Substance) == false) return;
        targetPart = targetPart.Skin ?? targetPart;
        targetPart.TryAddModifier(BodyPartModifierGenerator.Generate(Def, SpreadDuration.RandomValue, Power));
    }

    public override bool ApplyToPart(BodyPart part)
    {
        if (part.IsExternal == false) return false;
        if (AllowedSubstances.Contains(part.Substance) == false) return false;

        // Prefer to apply to skin if present
        var skin = part.Skin;
        if (skin != null)
        {
            skin.TryAddModifier(this);
        }
        else
        {
            part.TryAddModifier(this);
        }

        return true;
    }

    public override Widget? GetInfoPanel()
    {
        var isSkin = BodyPart?.Type == BodyPartType.Skin;
        var damageRange = isSkin ? SkinDamagePerTick : FleshDamagePerTick;

        return BuildInfoPanel(new InfoPanelData
        {
            Lines =
            [
                new($"-{damageRange.Min:0.#}-{damageRange.Max:0.#} damage/tick", new Color(200, 150, 100)),
                new("EXPLODES when expired!", InfoColors.Penetrated),
                new($"{SpreadChance * 100:0}% spread on pop", InfoColors.Spread)
            ],
            TimePrefix = "Time until pop",
            TimeColor = new Color(255, 150, 150)
        });
    }
}
