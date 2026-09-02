using Wendlemire.Definitions;
using Wendlemire.Sim.Cosmetics;

namespace Wendlemire.NetCode;

public static class CosmeticCatalog
{
    public static CosmeticDef? Get(string? moniker)
    {
        return string.IsNullOrWhiteSpace(moniker)
            ? null
            : DefRepository<CosmeticDef>.GetByMoniker(moniker, raiseError: false);
    }

    public static IEnumerable<CosmeticDef> All() => DefRepository<CosmeticDef>.Defs;

    public static IEnumerable<CosmeticDef> OfCategory(CosmeticCategory category) =>
        All().Where(def => def.Category == category);

    public static IEnumerable<CosmeticDef> DefaultOwned() =>
        All().Where(def => def.DefaultOwned);
}
