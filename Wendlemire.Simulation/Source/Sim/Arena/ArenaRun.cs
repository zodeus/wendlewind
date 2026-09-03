namespace Wendlemire.Sim.Arena;

public class ArenaRun : IExposable
{
    public const int StartingGold = 200;
    public const int WinGold = 200;
    public const int LoseGold = 150;
    public const int WinsToFinish = 12;
    public const int LossesToFinish = 5;

    public int RunSeed;
    public int Gold = StartingGold;
    public int Wins;
    public int Losses;
    public string PlayerId = "";
    public string PlayerName = "";
    public ArenaPhase Phase = ArenaPhase.GeneralStore;
    public MerchantDef? CurrentMerchant;
    public List<string> FoughtPlayerIds = [];
    public string? LastOpponentPlayerId;
    public bool LastFightWon;
    public int LastGoldDelta;
    public string ShopVisitKey = "";
    public List<PersistedShopShelf> ShopShelves = [];

    public event Action<ArenaPhase>? OnPhaseChanged;

    public int LivesRemaining => Math.Max(0, LossesToFinish - Losses);
    public bool IsRunOver => Wins >= WinsToFinish || Losses >= LossesToFinish;
    public bool IsVictory => Wins >= WinsToFinish;
    public int FightsPlayed => Wins + Losses;
    public int UpcomingRound => FightsPlayed + 1;

    public void Start(string playerId, string playerName, int runSeed)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        RunSeed = runSeed;
        Gold = StartingGold;
        Wins = 0;
        Losses = 0;
        FoughtPlayerIds = [];
        LastOpponentPlayerId = null;
        LastFightWon = false;
        LastGoldDelta = 0;
        ShopVisitKey = "";
        ShopShelves = [];
        CurrentMerchant = DefRepository<MerchantDef>.GetByMoniker("GeneralStore");
        SetPhase(ArenaPhase.GeneralStore);
    }

    public void SetPhase(ArenaPhase phase)
    {
        Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    public bool TryBuy(GameContext context, MerchantOffer offer, int quantity = 1)
    {
        if (quantity < 1)
        {
            return false;
        }

        var unitCost = offer.ResolveGoldCost();
        var cost = unitCost * quantity;
        if (unitCost < 0 || Gold < cost)
        {
            return false;
        }

        if (offer.IsUniqueOwnedType && (quantity != 1 || OwnsUnique(context.Player, offer.ItemDef!)))
        {
            return false;
        }

        var granted = offer.GrantedItems;
        if (granted.Count == 0)
        {
            return false;
        }

        var created = new List<Item>();
        foreach (var def in granted)
        {
            if (!TryGrantPurchase(context, def, quantity, created))
            {
                return false;
            }
        }

        Gold -= cost;
        RecordPurchase(offer, quantity);
        return true;
    }

    public void AssignNextMerchant()
    {
        CurrentMerchant = MerchantPool.Select(RunSeed, FightsPlayed);
    }

    public IReadOnlyList<RolledShelf> OpenShopVisit(MerchantDef merchant, IReadOnlyList<RolledShelf> rolled)
    {
        var key = ShopVisitKeyFor(merchant);
        if (ShopVisitKey == key && ShopShelves.Count > 0)
        {
            return ShopStock.Restore(merchant, ShopShelves);
        }

        ShopVisitKey = key;
        ShopShelves = ShopStock.Capture(rolled);
        return rolled;
    }

    public bool TryRefreshShelf(
        MerchantDef merchant,
        ShopCategory category,
        IReadOnlySet<string>? ownedUniqueMonikers = null)
    {
        PersistedShopShelf? persisted = null;
        foreach (var shelf in ShopShelves)
        {
            if (shelf.Category == category)
            {
                persisted = shelf;
                break;
            }
        }

        if (persisted == null)
        {
            return false;
        }

        MerchantShelf? shelfDef = null;
        foreach (var shelf in merchant.Shelves)
        {
            if (shelf.Category == category)
            {
                shelfDef = shelf;
                break;
            }
        }

        if (shelfDef == null)
        {
            return false;
        }

        var cost = ShopCatalog.ShelfRefreshCost(category, persisted.RefreshCount);
        if (Gold < cost)
        {
            return false;
        }

        var seed = ArenaSeeds.ShopRefresh(
            RunSeed,
            merchant.Moniker,
            FightsPlayed,
            category,
            persisted.RefreshCount + 1);
        var offers = ShopStock.RollShelf(shelfDef, new Random(seed), FightsPlayed, ownedUniqueMonikers);
        persisted.OfferKeys = offers.Select(offer => offer.StockKey).ToList();
        persisted.Remaining = offers.Select(offer => offer.Available).ToList();
        persisted.RefreshCount++;
        Gold -= cost;
        return true;
    }

    public void RecordPurchase(MerchantOffer offer, int quantity = 1)
    {
        if (quantity < 1 || ShopShelves.Count == 0)
        {
            return;
        }

        foreach (var shelf in ShopShelves)
        {
            for (var i = 0; i < shelf.OfferKeys.Count; i++)
            {
                if (shelf.OfferKeys[i] != offer.StockKey)
                {
                    continue;
                }

                var remaining = i < shelf.Remaining.Count ? shelf.Remaining[i] : 0;
                remaining = Math.Max(0, remaining - quantity);
                if (remaining <= 0)
                {
                    shelf.OfferKeys.RemoveAt(i);
                    if (i < shelf.Remaining.Count)
                    {
                        shelf.Remaining.RemoveAt(i);
                    }

                    return;
                }

                shelf.Remaining[i] = remaining;
                return;
            }
        }
    }

    public string ShopVisitKeyFor(MerchantDef merchant) => $"{merchant.Moniker}:{FightsPlayed}";

    public bool TrySell(GameContext context, Item item)
    {
        if (item.IsDestroyed)
        {
            return false;
        }

        if (!IsInInventory(context.PlayerPawn, item))
        {
            var unequipped = context.PlayerPawn.Equipment.UnEquip(item);
            if (unequipped == null || !context.PlayerPawn.Inventory.TryAdd(unequipped))
            {
                return false;
            }

            item = unequipped;
        }

        var payout = ShopCatalog.GetSellPrice(item.ItemDef);
        if (ShopCatalog.GetBuyPrice(item.ItemDef) <= 0)
        {
            return false;
        }

        var taken = context.PlayerPawn.Inventory.Take(item.Def, 1);
        if (taken == null)
        {
            return false;
        }

        taken.Destroy();
        Gold += payout;
        return true;
    }

    private static bool OwnsUnique(Player player, ItemDef def)
    {
        return ShopStock.OwnedUniqueMonikers(player).Contains(def.Moniker);
    }

    private static bool IsInInventory(Pawn pawn, Item item)
    {
        foreach (var owned in pawn.Inventory)
        {
            if (owned == item)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGrantPurchase(GameContext context, ItemDef def, int quantity, List<Item> created)
    {
        if (def.StackLimit > 1)
        {
            return TryAddPurchasedItem(context, context.Factory.CreateEntity<Item>(def, quantity), created);
        }

        for (var i = 0; i < quantity; i++)
        {
            if (!TryAddPurchasedItem(context, context.Factory.CreateEntity<Item>(def), created))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryAddPurchasedItem(GameContext context, Item item, List<Item> created)
    {
        if (context.PlayerPawn.Inventory.TryAdd(item))
        {
            created.Add(item);
            PurchaseAutoEquip.TryApply(context.PlayerPawn, item);
            return true;
        }

        RollbackPurchase(created);
        item.Destroy();
        return false;
    }

    private static void RollbackPurchase(List<Item> created)
    {
        foreach (var item in created)
        {
            item.Destroy();
        }
    }

    public void RecordMatchResult(bool playerWon, string opponentPlayerId)
    {
        LastFightWon = playerWon;
        LastOpponentPlayerId = opponentPlayerId;
        if (!string.IsNullOrEmpty(opponentPlayerId) && !FoughtPlayerIds.Contains(opponentPlayerId))
        {
            FoughtPlayerIds.Add(opponentPlayerId);
        }

        if (playerWon)
        {
            Wins++;
            LastGoldDelta = WinGold;
            Gold += WinGold;
        }
        else
        {
            Losses++;
            LastGoldDelta = LoseGold;
            Gold += LoseGold;
        }
    }

    public void AdvanceAfterMatch()
    {
        if (IsRunOver)
        {
            SetPhase(ArenaPhase.RunEnd);
            return;
        }

        AssignNextMerchant();
        SetPhase(ArenaPhase.MerchantSelect);
    }

    public void ApplyMatchResult(bool playerWon, string opponentPlayerId)
    {
        RecordMatchResult(playerWon, opponentPlayerId);
        AdvanceAfterMatch();
    }

    public void ExposeData()
    {
        ScribeValues.Look(ref RunSeed, "RunSeed");
        ScribeValues.Look(ref Gold, "Gold");
        ScribeValues.Look(ref Wins, "Wins");
        ScribeValues.Look(ref Losses, "Losses");
        var playerId = PlayerId;
        var playerName = PlayerName;
        var phase = Phase;
        ScribeValues.Look(ref playerId, "PlayerId", "");
        ScribeValues.Look(ref playerName, "PlayerName", "");
        ScribeValues.Look(ref phase, "Phase");
        PlayerId = playerId ?? "";
        PlayerName = playerName ?? "";
        Phase = phase;
        ScribeDefs.Look(ref CurrentMerchant, "CurrentMerchant");
        ScribeCollections.Look(ref FoughtPlayerIds!, "FoughtPlayerIds", LookMode.Value);
        var lastOpponent = LastOpponentPlayerId ?? "";
        ScribeValues.Look(ref lastOpponent, "LastOpponentPlayerId", "");
        LastOpponentPlayerId = string.IsNullOrEmpty(lastOpponent) ? null : lastOpponent;
        ScribeValues.Look(ref LastFightWon, "LastFightWon");
        ScribeValues.Look(ref LastGoldDelta, "LastGoldDelta");
        ScribeValues.Look(ref ShopVisitKey!, "ShopVisitKey", "");
        ScribeCollections.Look(ref ShopShelves!, "ShopShelves", LookMode.Deep);
        FoughtPlayerIds ??= [];
        ShopVisitKey ??= "";
        ShopShelves ??= [];
    }
}
