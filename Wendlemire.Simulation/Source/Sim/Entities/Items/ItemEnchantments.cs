﻿using System.Collections;

namespace Wendlemire.Sim.Entities.Items;

public class ItemEnchantments : IEnumerable<Item>, IExposable
{
    public int MaxEnchantments;
    
    public event Action? EnchantmentChanged;

    private Dictionary<int, Item?> _enchantments = null!;

    public void ExposeData()
    {
        ScribeValues.Look(ref MaxEnchantments, "MaxEnchantments");
        ScribeCollections.Look(ref _enchantments!, "Enchantments", LookMode.Value, LookMode.Deep);
    }

    public Item? TryGetAtSocket(int position)
    {
        return _enchantments.Count > position ? _enchantments[position] : null;
    }

    public void TryAdd(Item enchantment, int position = 0)
    {
        _enchantments[position] = enchantment;
        EnchantmentChanged?.Invoke();
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