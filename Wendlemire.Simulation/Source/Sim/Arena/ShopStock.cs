namespace Wendlemire.Sim.Arena;

public sealed class RolledShelf
{
    public required ShopCategory Category { get; init; }
    public required IReadOnlyList<MerchantOffer> Offers { get; init; }
    public int Columns { get; init; } = ShopLayout.GridColumns;
    public int ItemColumns { get; init; } = 1;
    public int RefreshCount { get; init; }
}

public sealed class PersistedShopShelf : IExposable
{
    public ShopCategory Category;
    public int Columns = ShopLayout.GridColumns;
    public int ItemColumns = 1;
    public int RefreshCount;
    public List<string> OfferKeys = [];
    public List<int> Remaining = [];

    public void ExposeData()
    {
        ScribeValues.Look(ref Category, "Category");
        ScribeValues.Look(ref Columns, "Columns");
        ScribeValues.Look(ref ItemColumns, "ItemColumns");
        ScribeValues.Look(ref RefreshCount, "RefreshCount");
        ScribeCollections.Look(ref OfferKeys!, "OfferKeys", LookMode.Value);
        ScribeCollections.Look(ref Remaining!, "Remaining", LookMode.Value);
        OfferKeys ??= [];
        Remaining ??= [];
    }
}

public static class ShopStock
{
    public static IReadOnlyList<RolledShelf> Roll(
        MerchantDef merchant,
        int runSeed,
        int round,
        IReadOnlySet<string>? ownedUniqueMonikers = null)
    {
        if (merchant.Shelves.Count == 0)
        {
            return [];
        }

        var seed = ArenaSeeds.Shop(runSeed, merchant.Moniker, round);
        var rng = new Random(seed);
        var rolled = new List<RolledShelf>(merchant.Shelves.Count);
        foreach (var shelf in merchant.Shelves)
        {
            rolled.Add(new RolledShelf
            {
                Category = shelf.Category,
                Offers = RollShelf(shelf, rng, round, ownedUniqueMonikers),
                Columns = shelf.ResolvedColumns,
                ItemColumns = shelf.ResolvedItemColumns
            });
        }

        return rolled;
    }

    public static HashSet<string> OwnedUniqueMonikers(Player player)
    {
        var owned = new HashSet<string>();
        foreach (var def in player.TrinketsFound)
        {
            if (def?.Moniker != null)
            {
                owned.Add(def.Moniker);
            }
        }

        var pawn = player.Pawn;
        foreach (var item in pawn.Inventory)
        {
            if (MerchantOffer.IsUniqueOwnedTypeDef(item.ItemDef) && item.Def.Moniker != null)
            {
                owned.Add(item.Def.Moniker);
            }
        }

        foreach (var incense in pawn.ActiveIncense)
        {
            if (!string.IsNullOrEmpty(incense.SourceMoniker))
            {
                owned.Add(incense.SourceMoniker);
            }
        }

        return owned;
    }

    public static List<PersistedShopShelf> Capture(IReadOnlyList<RolledShelf> shelves)
    {
        return shelves.Select(shelf => new PersistedShopShelf
        {
            Category = shelf.Category,
            Columns = shelf.Columns,
            ItemColumns = shelf.ItemColumns,
            RefreshCount = shelf.RefreshCount,
            OfferKeys = shelf.Offers.Select(offer => offer.StockKey).ToList(),
            Remaining = shelf.Offers.Select(offer => offer.Available).ToList()
        }).ToList();
    }

    public static IReadOnlyList<RolledShelf> Restore(
        MerchantDef merchant,
        IReadOnlyList<PersistedShopShelf> persisted)
    {
        var lookup = new Dictionary<string, MerchantOffer>();
        foreach (var offer in merchant.AllOffers)
        {
            lookup.TryAdd(offer.StockKey, offer);
        }

        return persisted.Select(shelf => new RolledShelf
        {
            Category = shelf.Category,
            Offers = RestoreOffers(shelf, lookup),
            Columns = shelf.Columns,
            ItemColumns = shelf.ItemColumns,
            RefreshCount = shelf.RefreshCount
        }).ToList();
    }

    public static IReadOnlyList<MerchantOffer> Flatten(IReadOnlyList<RolledShelf> shelves) =>
        shelves.SelectMany(shelf => shelf.Offers).ToList();

    public static IEnumerable<MerchantOffer> AvailableOffers(MerchantDef merchant, int round) =>
        merchant.AllOffers.Where(offer => offer.IsAvailable(round));

    public static IReadOnlyList<MerchantOffer> RollShelf(
        MerchantShelf shelf,
        Random rng,
        int round,
        IReadOnlySet<string>? ownedUniqueMonikers = null)
    {
        var available = shelf.Offers
            .Where(offer => offer.IsAvailable(round) && !IsOwnedUnique(offer, ownedUniqueMonikers))
            .ToList();
        if (available.Count == 0)
        {
            return [];
        }

        var sets = available.Where(offer => offer.IsSet).ToList();
        var pieces = available.Where(offer => !offer.IsSet).ToList();
        var stockSize = Math.Max(shelf.StockSize, 1);
        if (sets.Count == 0)
        {
            return CloneForStock(WeightedTake(pieces, stockSize, rng));
        }

        var pickedSets = WeightedTake(sets, 1, rng);
        var remaining = Math.Max(0, stockSize - pickedSets.Count);
        return CloneForStock([..pickedSets, ..WeightedTake(pieces, remaining, rng)]);
    }

    private static List<MerchantOffer> RestoreOffers(
        PersistedShopShelf shelf,
        IReadOnlyDictionary<string, MerchantOffer> lookup)
    {
        var offers = new List<MerchantOffer>();
        var count = Math.Min(shelf.OfferKeys.Count, shelf.Remaining.Count);
        for (var i = 0; i < count; i++)
        {
            if (shelf.Remaining[i] <= 0 || !lookup.TryGetValue(shelf.OfferKeys[i], out var template))
            {
                continue;
            }

            var clone = template.CloneForStock();
            clone.Available = shelf.Remaining[i];
            offers.Add(clone);
        }

        return offers;
    }

    private static List<MerchantOffer> CloneForStock(IEnumerable<MerchantOffer> offers) =>
        offers.Select(offer => offer.CloneForStock()).ToList();

    private static bool IsOwnedUnique(MerchantOffer offer, IReadOnlySet<string>? ownedUniqueMonikers)
    {
        return ownedUniqueMonikers != null
               && offer.IsUniqueOwnedType
               && offer.ItemDef?.Moniker != null
               && ownedUniqueMonikers.Contains(offer.ItemDef.Moniker);
    }

    private static List<MerchantOffer> WeightedTake(List<MerchantOffer> pool, int count, Random rng)
    {
        var take = Math.Min(Math.Max(count, 0), pool.Count);
        var remaining = pool.ToList();
        var picked = new List<MerchantOffer>(take);
        for (var n = 0; n < take; n++)
        {
            var index = PickWeightedIndex(remaining, rng);
            picked.Add(remaining[index]);
            remaining.RemoveAt(index);
        }

        return picked;
    }

    private static int PickWeightedIndex(IReadOnlyList<MerchantOffer> pool, Random rng)
    {
        var total = 0;
        foreach (var offer in pool)
        {
            total += offer.RollWeight;
        }

        var roll = rng.Next(total);
        var cumulative = 0;
        for (var i = 0; i < pool.Count; i++)
        {
            cumulative += pool[i].RollWeight;
            if (roll < cumulative)
            {
                return i;
            }
        }

        return pool.Count - 1;
    }
}
