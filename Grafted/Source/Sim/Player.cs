using Grafted.Sim.Entities;

namespace Grafted.Sim;

public class Player : IExposable {
    private Pawn _pawn = null!;

    public List<ItemDef> TrinketsFound = null!;
    public string Label => "You"; //Pawn.Label;
    public Pawn Pawn => _pawn;

    public void Initialize(Pawn pawn) {
        _pawn = pawn;
        TrinketsFound = new List<ItemDef>();
    }

    public void ResetPawn(Pawn pawn) {
        _pawn.Destroy();
        _pawn = pawn;
    }

    public void ExposeData() {
        ScribeDeep.Look(ref _pawn!, "Pawn");
        ScribeCollections.Look(ref TrinketsFound!, "TrinketsFound", LookMode.Def);
    }

    public bool HasTrinket(ItemDef def) {
        return TrinketsFound.Contains(def);
    }

    public IEnumerable<Entity> FindItems(Func<Item, bool> filter) {
        foreach (Item item in _pawn.Inventory) {
            if (filter(item)) {
                yield return item;
            }
        }
    }
}