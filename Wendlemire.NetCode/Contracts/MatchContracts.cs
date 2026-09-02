using System.Text.Json.Serialization;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Items.Medicinals;
using Wendlemire.Sim.Entities.Items.Potions;

namespace Wendlemire.NetCode.Contracts;

public sealed record BuildSnapshot
{
    public required string PlayerId { get; init; }
    public required string BuildId { get; init; }
    public required string[] EntityDefMonikers { get; init; }
    public int Seed { get; init; }
    public string PawnDefMoniker { get; init; } = "HumanA";
    public string? PawnName { get; init; }
    public string? Username { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
    public int Round { get; init; }
    public int Rating { get; init; }
    public string? StanceMoniker { get; init; }
    public WeaponConfig[] Weapons { get; init; } = [];
    public PotionConfig[] Potions { get; init; } = [];
    public SocketedItemConfig[] Sockets { get; init; } = [];
    public string[] FoodBuffs { get; init; } = [];
    public string[] Meal { get; init; } = [];
    public MedicalChestConfig[] MedicalChest { get; init; } = [];
    public IncenseConfig[] Incense { get; init; } = [];
    public InventoryStackConfig[] Inventory { get; init; } = [];
    public SkillConfig[] Skills
    {
        get => field ?? [];
        init => field = value ?? [];
    }
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

public sealed record MedicalChestConfig
{
    public required string ItemMoniker { get; init; }
    public int Charges { get; init; } = 1;
    public MedicalTriggerType Type { get; init; }
    public MedicalTargetSelector TargetSelector { get; init; }
    public float Threshold { get; init; }
    public float AfterSeconds { get; init; }
    public float HealthThreshold { get; init; } = 0.6f;
    public string? TargetPartKey { get; init; }
}

public sealed record IncenseConfig
{
    public required string ItemMoniker { get; init; }
    public int EncountersRemaining { get; init; }
    public float AfterSeconds { get; init; }
}

public sealed record InventoryStackConfig
{
    public required string ItemMoniker { get; init; }
    public int Amount { get; init; } = 99;
}

public sealed record SocketedItemConfig
{
    public required string ItemMoniker { get; init; }
    public string[] EnchantmentMonikers { get; init; } = [];
}

public sealed record SkillConfig
{
    public required string SkillMoniker { get; init; }
    public int Level { get; init; }
    public float CurrentLevelXp { get; init; }
}

public sealed record BuildPoolState
{
    public Dictionary<string, List<BuildSnapshot>> Rounds { get; init; } = new();
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
    public string? DefenderPlayerId { get; init; }
    public BuildSnapshot? Defender { get; init; }
    public int EncounterSeed { get; init; }
    public string? Version { get; init; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
[JsonSerializable(typeof(BuildSnapshot))]
[JsonSerializable(typeof(WeaponConfig))]
[JsonSerializable(typeof(PotionConfig))]
[JsonSerializable(typeof(MedicalChestConfig))]
[JsonSerializable(typeof(IncenseConfig))]
[JsonSerializable(typeof(InventoryStackConfig))]
[JsonSerializable(typeof(SocketedItemConfig))]
[JsonSerializable(typeof(SkillConfig))]
[JsonSerializable(typeof(MatchRequest))]
[JsonSerializable(typeof(CombatResult))]
[JsonSerializable(typeof(List<BuildSnapshot>))]
[JsonSerializable(typeof(BuildPoolState))]
[JsonSerializable(typeof(Dictionary<string, List<BuildSnapshot>>))]
[JsonSerializable(typeof(PlayerProfileRecord))]
[JsonSerializable(typeof(CreatePlayerRequest))]
[JsonSerializable(typeof(AchievementRecord))]
[JsonSerializable(typeof(AchievementState))]
[JsonSerializable(typeof(List<AchievementRecord>))]
[JsonSerializable(typeof(ArenaProgressRecord))]
[JsonSerializable(typeof(ShopShelfRecord))]
[JsonSerializable(typeof(List<ShopShelfRecord>))]
[JsonSerializable(typeof(ArenaFightRecord))]
[JsonSerializable(typeof(ArenaRunRecord))]
[JsonSerializable(typeof(List<ArenaRunRecord>))]
[JsonSerializable(typeof(StartArenaRequest))]
[JsonSerializable(typeof(PotionTriggerType))]
[JsonSerializable(typeof(MedicalTriggerType))]
[JsonSerializable(typeof(MedicalTargetSelector))]
[JsonSerializable(typeof(FightAnalytics))]
[JsonSerializable(typeof(FightSideStats))]
[JsonSerializable(typeof(CombatLogEvent))]
[JsonSerializable(typeof(CombatLogEvent[]))]
[JsonSerializable(typeof(CombatSubEffect))]
[JsonSerializable(typeof(CombatSubEffect[]))]
[JsonSerializable(typeof(CombatEventKind))]
[JsonSerializable(typeof(CombatLogRecord))]
[JsonSerializable(typeof(CombatEventsFile))]
[JsonSerializable(typeof(List<CombatLogRecord>))]
[JsonSerializable(typeof(FightAnalyticsRow))]
[JsonSerializable(typeof(List<FightAnalyticsRow>))]
[JsonSerializable(typeof(FightAnalyticsSummary))]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(BackfillResult))]
[JsonSerializable(typeof(AdminLoginRequest))]
[JsonSerializable(typeof(AdminSession))]
[JsonSerializable(typeof(AdminOverview))]
[JsonSerializable(typeof(AdminPoolRound))]
[JsonSerializable(typeof(List<AdminPoolRound>))]
[JsonSerializable(typeof(AdminPlayerRow))]
[JsonSerializable(typeof(List<AdminPlayerRow>))]
[JsonSerializable(typeof(AdminPlayerDetail))]
[JsonSerializable(typeof(AdminRunRow))]
[JsonSerializable(typeof(List<AdminRunRow>))]
[JsonSerializable(typeof(AdminPoolState))]
[JsonSerializable(typeof(ActivationCodeRecord))]
[JsonSerializable(typeof(List<ActivationCodeRecord>))]
[JsonSerializable(typeof(ActivationCodeFile))]
[JsonSerializable(typeof(CreateActivationCodesRequest))]
[JsonSerializable(typeof(ActivateRequest))]
[JsonSerializable(typeof(DownloadAsset))]
[JsonSerializable(typeof(List<DownloadAsset>))]
[JsonSerializable(typeof(DownloadCatalog))]
[JsonSerializable(typeof(ActivationCodeSummary))]
[JsonSerializable(typeof(HealthStatus))]
[JsonSerializable(typeof(VersionMismatchError))]
public partial class NetCodeJsonContext : JsonSerializerContext;
