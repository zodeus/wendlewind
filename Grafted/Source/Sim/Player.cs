namespace Grafted.Sim;

public class Player : IExposable
{
    private Pawn _pawn = null!;

    private List<ItemDef> _trinketsFound = null!;
    public string Label => "undefined";
    public Pawn Pawn => _pawn;

    public IReadOnlyList<ItemDef> TrinketsFound => _trinketsFound;

    public void Initialize()
    {
        Reset();
    }

    public void Reset()
    {
        _pawn?.Destroy();
        _trinketsFound = new List<ItemDef>();
        _pawn = GeneratePlayerPawn();
    }

    public void OnItemFound(Item item)
    {
        if (HasTrinket(item.ItemDef)) {
            Log.Warning($"Player already has trinket {item.ItemDef.Label}");
            return;
        }

        if (item.ItemDef.ItemType == ItemType.Trinket || item.ItemDef.TrinketProperties != null)
        {
            _trinketsFound.Add(item.ItemDef);
        }
    }

    public void ExposeData()
    {
        ScribeDeep.Look(ref _pawn!, "Pawn");
        ScribeCollections.Look(ref _trinketsFound!, "TrinketsFound", LookMode.Def);
    }

    public IEnumerable<Entity> FindItems(Func<Item, bool> filter)
    {
        foreach (Item item in _pawn.Inventory)
        {
            if (filter(item))
            {
                yield return item;
            }
        }
    }

    public bool HasTrinkets(params ItemDef[] items)
    {
        return items.Intersect(_trinketsFound).Count() == items.Length;
    }

    public bool HasTrinket(ItemDef trinket)
    {
        return _trinketsFound.Contains(trinket);
    }

    private static Pawn GeneratePlayerPawn()
    {
        var pawn = PawnGenerator.CreatePawn(
            new PawnRequest($"Human (specimen Alpha)",
            DefRepository<PawnDef>.GetByMoniker("HumanA")!,
            Defs.PawnLoadouts.DefaultStarterLoadout, PawnType.Player)
        );

        return pawn;
    }
}