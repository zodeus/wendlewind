using System.Collections;

namespace Grafted.Sim.Entities.Items;

public class ItemEnchantments : IEnumerable<Item>, IExposable
{
    public int MaxEnchantments;

    private Dictionary<int, Item?> _enchantments = null!;

    public void ExposeData()
    {
        ScribeValues.Look(ref MaxEnchantments!, "MaxEnchantments");
        ScribeCollections.Look(ref _enchantments!, "Enchantments", LookMode.Value, LookMode.Deep);
    }

    public Item? TryGetAtSlot(int position)
    {
        return _enchantments.Count > position ? _enchantments[position] : null;
    }

    public void TryAdd(Item enchantment, int position = 0)
    {
        _enchantments[position] = enchantment;
    }

    public void Initialize()
    {
        _enchantments = new Dictionary<int, Item?>();
        for (var i = 0; i < MaxEnchantments; i++)
        {
            _enchantments[i] = null;
        }
    }

    public IEnumerator<Item> GetEnumerator()
    {
        return _enchantments.Values.Where(i => i != null).GetEnumerator()!;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}