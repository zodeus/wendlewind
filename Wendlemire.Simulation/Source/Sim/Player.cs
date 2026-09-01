﻿namespace Wendlemire.Sim;

public class Player : IExposable, IHasContext
{
    public GameContext Context { get; set; } = null!;
    private Pawn _pawn = null!;

    private List<ItemDef> _trinketsFound = null!;
    public string Username = "";
    public string Label => string.IsNullOrWhiteSpace(Username) ? "undefined" : Username;
    public Pawn Pawn => _pawn;

    public IReadOnlyList<ItemDef> TrinketsFound => _trinketsFound;

    public void Initialize(string? username = null)
    {
        SetUsername(username);
        Reset();
    }

    public void Reset()
    {
        _pawn?.Destroy();
        _trinketsFound = new List<ItemDef>();
        _pawn = GeneratePlayerPawn();
    }

    public void ResetForArena(string name, string? pawnDefMoniker = null)
    {
        SetUsername(name);
        _pawn?.Destroy();
        _trinketsFound = new List<ItemDef>();
        var empty = DefRepository<PawnLoadoutDef>.GetByMoniker("EmptyLoadout")
                    ?? Defs.PawnLoadouts.DefaultStarterLoadout;
        var pawnDef = DefRepository<PawnDef>.GetByMoniker(pawnDefMoniker ?? "HumanA", raiseError: false)
                      ?? DefRepository<PawnDef>.GetByMoniker("HumanA")!;
        _pawn = PawnGenerator.CreatePawn(
            Context,
            new PawnRequest(
                ResolvePawnName(),
                pawnDef,
                empty,
                PawnType.Player));
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
        var username = Username;
        ScribeValues.Look(ref username, "Username", "");
        Username = username ?? "";
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

    private Pawn GeneratePlayerPawn()
    {
        var pawn = PawnGenerator.CreatePawn(
            Context,
            new PawnRequest(
                ResolvePawnName(),
                DefRepository<PawnDef>.GetByMoniker("HumanA")!,
                Defs.PawnLoadouts.DefaultStarterLoadout,
                PawnType.Player)
        );

        return pawn;
    }

    private void SetUsername(string? username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            Username = username.Trim();
        }
    }

    private string ResolvePawnName() =>
        string.IsNullOrWhiteSpace(Username) ? "UnnamedPlayer" : Username;
}