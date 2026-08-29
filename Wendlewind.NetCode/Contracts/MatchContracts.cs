using System.Text.Json.Serialization;

namespace Wendlewind.NetCode.Contracts;

public sealed record BuildSnapshot
{
    public required string PlayerId { get; init; }
    public required string BuildId { get; init; }
    public required string[] EntityDefMonikers { get; init; }
    public int Seed { get; init; }
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
[JsonSerializable(typeof(MatchRequest))]
[JsonSerializable(typeof(CombatResult))]
public partial class NetCodeJsonContext : JsonSerializerContext;
