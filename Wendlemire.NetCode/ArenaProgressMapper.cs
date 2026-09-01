using Wendlemire.Definitions;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Sim.Arena;

namespace Wendlemire.NetCode;

public static class ArenaProgressMapper
{
    public static ArenaProgressRecord FromRun(
        ArenaRun run,
        BuildSnapshot? loadout,
        string runId,
        DateTimeOffset startedAt)
    {
        return new ArenaProgressRecord
        {
            RunId = runId,
            PlayerId = run.PlayerId,
            PlayerName = run.PlayerName,
            RunSeed = run.RunSeed,
            Gold = run.Gold,
            Wins = run.Wins,
            Losses = run.Losses,
            Phase = run.Phase.ToString(),
            CurrentMerchantMoniker = run.CurrentMerchant?.Moniker,
            FoughtPlayerIds = [..run.FoughtPlayerIds],
            LastOpponentPlayerId = run.LastOpponentPlayerId,
            LastFightWon = run.LastFightWon,
            LastGoldDelta = run.LastGoldDelta,
            ShopVisitKey = run.ShopVisitKey,
            ShopShelves = [..run.ShopShelves.Select(ToRecord)],
            Loadout = loadout,
            StartedAt = startedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static void ApplyTo(ArenaRun run, ArenaProgressRecord record)
    {
        run.PlayerId = record.PlayerId;
        run.PlayerName = record.PlayerName;
        run.RunSeed = record.RunSeed;
        run.Gold = record.Gold;
        run.Wins = record.Wins;
        run.Losses = record.Losses;
        run.FoughtPlayerIds = [..record.FoughtPlayerIds];
        run.LastOpponentPlayerId = record.LastOpponentPlayerId;
        run.LastFightWon = record.LastFightWon;
        run.LastGoldDelta = record.LastGoldDelta;
        run.ShopVisitKey = record.ShopVisitKey ?? "";
        run.ShopShelves = [..(record.ShopShelves ?? []).Select(FromRecord)];
        var merchantMoniker = record.CurrentMerchantMoniker == "WitchDoctor"
            ? "Alchemist"
            : record.CurrentMerchantMoniker;
        run.CurrentMerchant = string.IsNullOrWhiteSpace(merchantMoniker)
            ? null
            : DefRepository<MerchantDef>.GetByMoniker(merchantMoniker, raiseError: false);
        run.SetPhase(Enum.TryParse<ArenaPhase>(record.Phase, out var phase) ? phase : ArenaPhase.GeneralStore);
    }

    private static ShopShelfRecord ToRecord(PersistedShopShelf shelf)
    {
        return new ShopShelfRecord
        {
            Category = shelf.Category.ToString(),
            Columns = shelf.Columns,
            ItemColumns = shelf.ItemColumns,
            RefreshCount = shelf.RefreshCount,
            OfferKeys = [..shelf.OfferKeys],
            Remaining = [..shelf.Remaining]
        };
    }

    private static PersistedShopShelf FromRecord(ShopShelfRecord record)
    {
        return new PersistedShopShelf
        {
            Category = Enum.TryParse<ShopCategory>(record.Category, out var category) ? category : default,
            Columns = record.Columns,
            ItemColumns = record.ItemColumns,
            RefreshCount = record.RefreshCount,
            OfferKeys = [..record.OfferKeys],
            Remaining = [..record.Remaining]
        };
    }
}
