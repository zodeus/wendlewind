namespace Wendlewind.Sim.Arena;

public class ArenaRun : IExposable
{
    public const int StartingGold = 100;
    public const int WinGold = 100;
    public const int LoseGold = 75;
    public const int WinsToFinish = 10;
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

    public event Action<ArenaPhase>? OnPhaseChanged;

    public int LivesRemaining => Math.Max(0, LossesToFinish - Losses);
    public bool IsRunOver => Wins >= WinsToFinish || Losses >= LossesToFinish;
    public bool IsVictory => Wins >= WinsToFinish;
    public int FightsPlayed => Wins + Losses;

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
        return true;
    }

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

    public void ApplyMatchResult(bool playerWon, string opponentPlayerId)
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

        SetPhase(IsRunOver ? ArenaPhase.RunEnd : ArenaPhase.Results);
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
        ScribeCollections.Look(ref FoughtPlayerIds, "FoughtPlayerIds", LookMode.Value);
        var lastOpponent = LastOpponentPlayerId ?? "";
        ScribeValues.Look(ref lastOpponent, "LastOpponentPlayerId", "");
        LastOpponentPlayerId = string.IsNullOrEmpty(lastOpponent) ? null : lastOpponent;
        ScribeValues.Look(ref LastFightWon, "LastFightWon");
        ScribeValues.Look(ref LastGoldDelta, "LastGoldDelta");
    }
}
