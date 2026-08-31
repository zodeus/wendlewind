namespace Wendlewind.Sim.Arena;

public static class ShopLayout
{
    public const int GridColumns = 12;

    public static int NormalizeColumns(int columns) =>
        columns is >= 1 and <= GridColumns ? columns : GridColumns;

    public static int NormalizeItemColumns(int itemColumns, int shelfColumns)
    {
        var columns = NormalizeColumns(shelfColumns);
        var span = itemColumns < 1 ? 1 : itemColumns;
        if (span > columns)
        {
            span = columns;
        }

        while (columns % span != 0 && span > 1)
        {
            span--;
        }

        return span;
    }

    public static int SlotsPerRow(int shelfColumns, int itemColumns) =>
        NormalizeColumns(shelfColumns) / NormalizeItemColumns(itemColumns, shelfColumns);

    public static IReadOnlyList<IReadOnlyList<T>> GroupRows<T>(
        IReadOnlyList<T> shelves,
        Func<T, int> getColumns)
    {
        var rows = new List<List<T>>();
        var current = new List<T>();
        var used = 0;
        foreach (var shelf in shelves)
        {
            var columns = NormalizeColumns(getColumns(shelf));
            if (used > 0 && used + columns > GridColumns)
            {
                rows.Add(current);
                current = [];
                used = 0;
            }

            current.Add(shelf);
            used += columns;
            if (used >= GridColumns)
            {
                rows.Add(current);
                current = [];
                used = 0;
            }
        }

        if (current.Count > 0)
        {
            rows.Add(current);
        }

        return rows;
    }
}
