namespace Wendlemire.Sim.Combat;

/// <summary>
/// Arena fight-length knobs. Applied to human external flesh at body adapt;
/// internals inherit via <see cref="MaxHitPointScalerConstantFactor"/>.
/// </summary>
public static class CombatBalance
{
    public const float VitalHpScale = 0.85f;
    public const float LimbHpScale = 0.78f;

    /// <summary>
    /// Diminishing-returns armor: reduction = resist / (resist + ArmorK). Never 100%.
    /// Leather 18 ≈ 11% DR, chain 34 ≈ 20% DR, plate 48 ≈ 26% DR, witch doctor 48 ≈ 26% DR.
    /// </summary>
    public const float ArmorK = 140f;

    /// <summary>
    /// Attack speed = base / (1 + equippedWeight * this). Full cloth (~11) ≈ 6% slower, full plate (~77) ≈ 32%.
    /// </summary>
    public const float WeightAttackSpeedFactor = 0.006f;

    public const float ArmoredDotChanceFactor = 0.65f;
    public const float ArmoredDotPowerFactor = 0.7f;

    public static float ArmorReduction(float resist)
    {
        if (resist <= 0f)
        {
            return 0f;
        }

        return resist / (resist + ArmorK);
    }

    public static float ArmorPassThrough(float resist) => 1f - ArmorReduction(resist);

    public static double BlockedAmount(double totalDamage, float resist) =>
        totalDamage * ArmorReduction(resist);

    public static float ScaleFor(BodyPartType type) => type switch
    {
        BodyPartType.Head or BodyPartType.Neck or BodyPartType.Torso => VitalHpScale,
        BodyPartType.Arm or BodyPartType.Hand or BodyPartType.Finger or BodyPartType.Thumb
            or BodyPartType.Leg or BodyPartType.Foot or BodyPartType.Toe => LimbHpScale,
        _ => 1f
    };
}
