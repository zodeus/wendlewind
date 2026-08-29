using System.Text.Json.Serialization;
using Wendlewind.Sim.Entities.Items.Potions;

namespace Wendlewind.NetCode.Contracts;

public sealed record BuildSnapshot
{
    public required string PlayerId { get; init; }
    public required string BuildId { get; init; }
    public required string[] EntityDefMonikers { get; init; }
    public int Seed { get; init; }
    public string? StanceMoniker { get; init; }
    public WeaponConfig[] Weapons { get; init; } = [];
    public PotionConfig[] Potions { get; init; } = [];
}

public sealed record WeaponConfig
{
    public required string ItemMoniker { get; init; }
    public bool UseInCombat { get; init; } = true;
}

public sealed record PotionConfig
{
    public required string ItemMoniker { get; init; }
    public PotionTriggerType Type { get; init; }
    public float Threshold { get; init; }
    public float AfterSeconds { get; init; }
    public float HealthThreshold { get; init; } = 0.6f;
}

public sealed record MatchRequest
{
    public required BuildSnapshot Attacker { get; init; }
    public BuildSnapshot? Defender { get; init; }
}

public sealed record CombatResult
{
    public required string MatchId { get; init; }
    public required string WinnerPlayerId { get; init; }
    public int Ticks { get; init; }
    public string? CauseOfDeath { get; init; }
}

[JsonSerializable(typeof(BuildSnapshot))]
[JsonSerializable(typeof(WeaponConfig))]
[JsonSerializable(typeof(PotionConfig))]
[JsonSerializable(typeof(MatchRequest))]
[JsonSerializable(typeof(CombatResult))]
[JsonSerializable(typeof(PotionTriggerType))]
public partial class NetCodeJsonContext : JsonSerializerContext;
