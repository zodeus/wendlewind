namespace Grafted.Sim;

public class Player : IExposable
{
    private Pawn _pawn = null!;

    public List<ItemDef> TrinketsFound = null!;
    public string Label => "undefined";
    public Pawn Pawn => _pawn;

    public void Initialize()
    {
        Reset();
    }

    public void Reset()
    {
        _pawn?.Destroy();
        TrinketsFound = new List<ItemDef>();
        _pawn = GeneratePlayerPawn();
    }

    public void ExposeData()
    {
        ScribeDeep.Look(ref _pawn!, "Pawn");
        ScribeCollections.Look(ref TrinketsFound!, "TrinketsFound", LookMode.Def);
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
        return items.Intersect(TrinketsFound).Count() == items.Length;
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