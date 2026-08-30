namespace Wendlewind.Sim.Combat;

public enum CombatEventKind
{
    Damage,
    Block,
    Miss,
    Dodge,
    Heal,
    DamageOverTime,
    BuffApplied,
    DebuffApplied,
    PartSevered,
    PartDestroyed,
    EquipmentDestroyed,
    StatusReflected,
    PotionUsed,
    MedicalUsed,
    Death,
    System
}

/// <summary>
/// Nested effect produced by a single hit (modifiers, severs, reflected procs, etc.).
/// Plain data only — no live object references.
/// </summary>
public sealed record CombatSubEffect
{
    public CombatEventKind Kind { get; init; }
    public int SubjectPawnId { get; init; }
    public string? SubjectName { get; init; }
    public string? BodyPartKey { get; init; }
    public string? BodyPartLabel { get; init; }
    public string? Label { get; init; }
    public string? ItemMoniker { get; init; }
    public string? ItemLabel { get; init; }
    public bool IsVital { get; init; }
}

/// <summary>
/// Authoritative, serializable combat-log entry. Keyed by stable IDs/monikers with
/// denormalized display fields. No engine or MonoGame types.
/// </summary>
public sealed record CombatLogEvent
{
    public CombatEventKind Kind { get; init; }
    public int Tick { get; init; }
    public int SubjectPawnId { get; init; }
    public string SubjectName { get; init; } = "";
    public int? SourcePawnId { get; init; }
    public string? SourceName { get; init; }
    public string? ItemMoniker { get; init; }
    public string? ItemLabel { get; init; }
    public string? WeaponManeuverLabel { get; init; }
    public string? BodyPartKey { get; init; }
    public string? BodyPartLabel { get; init; }
    public double Amount { get; init; }
    public double Blocked { get; init; }
    public string? DamageType { get; init; }
    public bool IsCritical { get; init; }
    public bool IsTrinket { get; init; }
    public string? Message { get; init; }
    public CombatSubEffect[] SubEffects { get; init; } = [];
}
