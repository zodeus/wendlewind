namespace Wendlemire.Sim.Combat;

/// <summary>
/// Arena fight-length knobs. Applied to human external flesh at body adapt;
/// internals inherit via <see cref="MaxHitPointScalerConstantFactor"/>.
/// </summary>
public static class CombatBalance
{
    public const float VitalHpScale = 0.96f;
    public const float LimbHpScale = 0.85f;

    public static float ScaleFor(BodyPartType type) => type switch
    {
        BodyPartType.Head or BodyPartType.Neck or BodyPartType.Torso => VitalHpScale,
        BodyPartType.Arm or BodyPartType.Hand or BodyPartType.Finger or BodyPartType.Thumb
            or BodyPartType.Leg or BodyPartType.Foot or BodyPartType.Toe => LimbHpScale,
        _ => 1f
    };
}
