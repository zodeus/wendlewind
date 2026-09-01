namespace Wendlemire.Sim.Arena;

public class MerchantShelf
{
    public ShopCategory Category;
    public int StockSize = 6;
    public int Columns = ShopLayout.GridColumns;
    public int ItemColumns = 1;
    public List<MerchantOffer> Offers = [];

    public int ResolvedColumns => ShopLayout.NormalizeColumns(Columns);
    public int ResolvedItemColumns => ShopLayout.NormalizeItemColumns(ItemColumns, ResolvedColumns);
}
