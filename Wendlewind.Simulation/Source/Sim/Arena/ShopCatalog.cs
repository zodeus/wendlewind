namespace Wendlewind.Sim.Arena;

public static class ShopCatalog
{
    public const float SetDiscount = 0.8f;
    public const int SellDivisor = 10;

    public static int GetBuyPrice(ItemDef def, MerchantDef? merchant = null)
    {
        if (merchant != null)
        {
            var local = FindPieceOffer(merchant, def);
            if (local != null)
            {
                return local.GoldCost;
            }
        }

        foreach (var other in DefRepository<MerchantDef>.Defs)
        {
            var offer = FindPieceOffer(other, def);
            if (offer != null)
            {
                return offer.GoldCost;
            }
        }

        return 0;
    }

    public static int GetSellPrice(ItemDef def, MerchantDef? merchant = null) =>
        GetBuyPrice(def, merchant) / SellDivisor;

    public static int ComputeSetCost(IReadOnlyList<ItemDef> pieces, MerchantDef merchant)
    {
        var sum = 0;
        foreach (var piece in pieces)
        {
            sum += GetBuyPrice(piece, merchant);
        }

        return (int)Math.Floor(sum * SetDiscount);
    }

    public static MerchantOffer? FindPieceOffer(MerchantDef merchant, ItemDef def)
    {
        foreach (var offer in merchant.AllOffers)
        {
            if (!offer.IsSet && offer.ItemDef == def)
            {
                return offer;
            }
        }

        return null;
    }
}
