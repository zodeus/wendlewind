namespace Wendlemire.Sim.Arena;

public class MerchantOffer
{
    public const int MedicalStock = 5;

    public ItemDef? ItemDef;
    public string? SetLabel;
    public List<ItemDef> SetPieces = [];
    public int GoldCost;
    public int AvailableFromRound;
    public int Weight;
    public int Available = 1;

    public bool IsSet => SetPieces.Count > 0;
    public bool IsUniqueOwnedType => !IsSet && IsUniqueOwnedTypeDef(ItemDef);

    public static bool IsUniqueOwnedTypeDef(ItemDef? def) =>
        def is { ItemType: ItemType.Food or ItemType.Incense or ItemType.Trinket }
        || def?.TrinketProperties != null;

    public int ResolveGoldCost()
    {
        if (GoldCost > 0)
        {
            return GoldCost;
        }

        return IsSet
            ? ShopCatalog.ComputeSetCost(SetPieces)
            : ItemDef?.GoldCost ?? 0;
    }

    public bool IsAvailable(int round) => AvailableFromRound <= round;

    public int RollWeight => Weight > 0 ? Weight : AvailableFromRound + 1;

    public string DisplayLabel => IsSet
        ? (string.IsNullOrWhiteSpace(SetLabel) ? "Armor Set" : SetLabel)
        : ItemDef?.Label ?? "";

    public string StockKey => IsSet
        ? $"set:{SetLabel}"
        : ItemDef?.Moniker ?? "";

    public IReadOnlyList<ItemDef> GrantedItems => IsSet
        ? SetPieces
        : ItemDef != null
            ? [ItemDef]
            : [];

    public MerchantOffer CloneForStock()
    {
        return new MerchantOffer
        {
            ItemDef = ItemDef,
            SetLabel = SetLabel,
            SetPieces = [..SetPieces],
            GoldCost = GoldCost,
            AvailableFromRound = AvailableFromRound,
            Weight = Weight,
            Available = ItemDef is { ItemType: ItemType.Medical } ? MedicalStock : 1
        };
    }
}
