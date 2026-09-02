using Wendlemire.Sim.Combat;

namespace Wendlemire.NetCode.Contracts;

public sealed record PlayerProfileRecord
{
    public required string PlayerId { get; init; }
    public string DisplayName { get; init; } = "Bilbert";
    public string Username { get; init; } = "";
    public int Rating { get; init; }
    public int RatedRuns { get; init; }
    public int PeakRating { get; init; }
    public int? LegendNumber { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record CreatePlayerRequest
{
    public string? PlayerId { get; init; }
    public string? DisplayName { get; init; }
    public string? Username { get; init; }
}

public sealed record AchievementRecord
{
    public required string Moniker { get; init; }
    public float CurrentValue { get; init; }
    public bool IsUnlocked { get; init; }
    public DateTimeOffset? UnlockedAt { get; init; }
    public bool IsAcknowledged { get; init; }
}

public sealed record AchievementState
{
    public List<AchievementRecord> Achievements { get; init; } = [];
}

public sealed record ArenaProgressRecord
{
    public required string RunId { get; init; }
    public required string PlayerId { get; init; }
    public string PlayerName { get; init; } = "";
    public int RunSeed { get; init; }
    public int Gold { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public string Phase { get; init; } = "GeneralStore";
    public string? CurrentMerchantMoniker { get; init; }
    public List<string> FoughtPlayerIds { get; init; } = [];
    public string? LastOpponentPlayerId { get; init; }
    public bool LastFightWon { get; init; }
    public int LastGoldDelta { get; init; }
    public string ShopVisitKey { get; init; } = "";
    public List<ShopShelfRecord> ShopShelves { get; init; } = [];
    public BuildSnapshot? Loadout { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? Version { get; init; }
}

public sealed record ShopShelfRecord
{
    public string Category { get; init; } = "";
    public int Columns { get; init; }
    public int ItemColumns { get; init; } = 1;
    public int RefreshCount { get; init; }
    public List<string> OfferKeys { get; init; } = [];
    public List<int> Remaining { get; init; } = [];
}

public sealed record ArenaFightRecord
{
    public required string MatchId { get; init; }
    public int Round { get; init; }
    public required BuildSnapshot Attacker { get; init; }
    public required BuildSnapshot Defender { get; init; }
    public int EncounterSeed { get; init; }
    public required string WinnerPlayerId { get; init; }
    public int Ticks { get; init; }
    public string? CauseOfDeath { get; init; }
    public DateTimeOffset FoughtAt { get; init; }
    public FightAnalytics? Analytics { get; init; }
    public string? Version { get; init; }
}

public sealed record ArenaRunRecord
{
    public required string RunId { get; init; }
    public required string PlayerId { get; init; }
    public string PlayerName { get; init; } = "";
    public int RunSeed { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public bool? Victory { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int FinalGold { get; init; }
    public int? RatingBefore { get; init; }
    public int? RatingAfter { get; init; }
    public int? RatingDelta { get; init; }
    public bool RankApplied { get; init; }
    public List<ArenaFightRecord> Fights { get; init; } = [];
    public string? Version { get; init; }
}

public sealed record StartArenaRequest
{
    public int? RunSeed { get; init; }
    public string? PlayerName { get; init; }
}

public sealed record CombatLogRecord
{
    public required string MatchId { get; init; }
    public CombatLogEvent[] Events { get; init; } = [];
    public string? Version { get; init; }
}

public sealed record CombatEventsFile
{
    public List<CombatLogRecord> Fights { get; init; } = [];
    public string? Version { get; init; }
}

public sealed record FightAnalyticsRow
{
    public required string MatchId { get; init; }
    public required string PlayerId { get; init; }
    public string PlayerName { get; init; } = "";
    public required string RunId { get; init; }
    public int Round { get; init; }
    public double DurationSeconds { get; init; }
    public bool InTargetBand { get; init; }
    public required string WinnerPlayerId { get; init; }
    public string WinnerName { get; init; } = "";
    public string? OpponentPlayerId { get; init; }
    public string OpponentName { get; init; } = "";
    public DateTimeOffset FoughtAt { get; init; }
    public string? CauseOfDeath { get; init; }
    public double AttackerDamagePerSecond { get; init; }
    public double DefenderDamagePerSecond { get; init; }
    public double AttackerDamage { get; init; }
    public double DefenderDamage { get; init; }
    public double AttackerHealing { get; init; }
    public double DefenderHealing { get; init; }
    public string? KillingWeapon { get; init; }
    public string? KillingManeuver { get; init; }
    public string? Version { get; init; }
}

public sealed record FightAnalyticsSummary
{
    public int Count { get; init; }
    public double InTargetBandPercent { get; init; }
    public double? DurationP50 { get; init; }
    public double? DurationP90 { get; init; }
    public double? DurationMin { get; init; }
    public double? DurationMax { get; init; }
    public Dictionary<string, int> CauseOfDeath { get; init; } = new();
    public string? LongestMatchId { get; init; }
    public string? ShortestMatchId { get; init; }
}

public sealed record BackfillResult
{
    public int Scanned { get; init; }
    public int Updated { get; init; }
    public int Skipped { get; init; }
}

public sealed record AdminLoginRequest
{
    public string? Password { get; init; }
}

public sealed record AdminSession
{
    public bool Authenticated { get; init; }
}

public sealed record AdminOverview
{
    public int Players { get; init; }
    public int ActiveArenas { get; init; }
    public int Runs { get; init; }
    public int FinishedRuns { get; init; }
    public int Victories { get; init; }
    public int Defeats { get; init; }
    public int Abandoned { get; init; }
    public int Fights { get; init; }
    public int PoolBuilds { get; init; }
    public int ActivationCodes { get; init; }
    public int UnusedCodes { get; init; }
    public List<AdminPoolRound> PoolByRound { get; init; } = [];
    public FightAnalyticsSummary FightSummary { get; init; } = new();
    public List<AdminPlayerRow> ActivePlayers { get; init; } = [];
}

public sealed record AdminPoolRound
{
    public int Round { get; init; }
    public int Builds { get; init; }
}

public sealed record AdminPlayerRow
{
    public required string PlayerId { get; init; }
    public string DisplayName { get; init; } = "";
    public string Username { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int RunCount { get; init; }
    public int FightCount { get; init; }
    public int TotalWins { get; init; }
    public int TotalLosses { get; init; }
    public int Victories { get; init; }
    public bool HasActiveArena { get; init; }
    public string? ActivePhase { get; init; }
    public int? ActiveWins { get; init; }
    public int? ActiveLosses { get; init; }
    public int? ActiveGold { get; init; }
    public int Rating { get; init; }
    public int RatedRuns { get; init; }
    public DateTimeOffset? LastPlayedAt { get; init; }
}

public sealed record AdminPlayerDetail
{
    public required AdminPlayerRow Player { get; init; }
    public AchievementState Achievements { get; init; } = new();
    public ArenaProgressRecord? CurrentArena { get; init; }
    public List<AdminRunRow> Runs { get; init; } = [];
}

public sealed record AdminRunRow
{
    public required string RunId { get; init; }
    public required string PlayerId { get; init; }
    public string PlayerName { get; init; } = "";
    public int RunSeed { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public bool? Victory { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int FinalGold { get; init; }
    public int FightCount { get; init; }
    public bool IsActive { get; init; }
    public string? Version { get; init; }
}

public sealed record AdminPoolState
{
    public int Count { get; init; }
    public List<AdminPoolRound> Rounds { get; init; } = [];
    public List<BuildSnapshot> Builds { get; init; } = [];
}

public sealed record ActivationCodeRecord
{
    public required string Id { get; init; }
    public required string Code { get; init; }
    public string Note { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? RedeemedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }
}

public sealed record ActivationCodeFile
{
    public string Secret { get; init; } = "";
    public List<ActivationCodeRecord> Codes { get; init; } = [];
}

public sealed record CreateActivationCodesRequest
{
    public int Count { get; init; } = 1;
    public string? Note { get; init; }
}

public sealed record ActivateRequest
{
    public string? Code { get; init; }
}

public sealed record DownloadAsset
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Detail { get; init; }
}

public sealed record DownloadCatalog
{
    public bool Unlocked { get; init; }
    public string? Version { get; init; }
    public string? Error { get; init; }
    public List<DownloadAsset> Assets { get; init; } = [];
}

public sealed record HealthStatus
{
    public string Status { get; init; } = "ok";
    public string Version { get; init; } = "";
    public int Zones { get; init; }
    public int Pawns { get; init; }
    public string? Player { get; init; }
    public int Pool { get; init; }
    public string? Data { get; init; }
}

public sealed record VersionMismatchError
{
    public string Error { get; init; } = "";
    public string Code { get; init; } = "version_mismatch";
    public string? ServerVersion { get; init; }
    public string? ClientVersion { get; init; }
}

public sealed record ActivationCodeSummary
{
    public int Total { get; init; }
    public int Unused { get; init; }
    public int Redeemed { get; init; }
    public int Revoked { get; init; }
}
