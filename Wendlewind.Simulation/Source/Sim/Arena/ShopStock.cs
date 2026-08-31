namespace Wendlewind.Sim.Arena;

public sealed class RolledShelf
{
    public required ShopCategory Category { get; init; }
    public required IReadOnlyList<MerchantOffer> Offers { get; init; }
    public int Columns { get; init; } = ShopLayout.GridColumns;
    public int ItemColumns { get; init; } = 1;
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

    public static IReadOnlyList<MerchantOffer> Flatten(IReadOnlyList<RolledShelf> shelves) =>
        shelves.SelectMany(shelf => shelf.Offers).ToList();

    public static IEnumerable<MerchantOffer> AvailableOffers(MerchantDef merchant, int round) =>
        merchant.AllOffers.Where(offer => offer.IsAvailable(round));

    private static IReadOnlyList<MerchantOffer> RollShelf(
        MerchantShelf shelf,
        Random rng,
        int round,
        IReadOnlySet<string>? ownedUniqueMonikers)
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

        var remaining = Math.Max(0, stockSize - sets.Count);
        return CloneForStock([..sets, ..WeightedTake(pieces, remaining, rng)]);
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
