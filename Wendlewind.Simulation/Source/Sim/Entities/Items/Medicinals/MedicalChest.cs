namespace Wendlewind.Sim.Entities.Items.Medicinals;

public class MedicalChestSlot : IExposable
{
    public ItemDef Def = null!;
    public int Charges;
    public MedicalTrigger Trigger = new();
    public int NextReadyTick;

    public bool IsInfinite => MedicalChest.IsInfiniteUse(Def);
    public bool HasCharge => IsInfinite || Charges > 0;

    public void ExposeData()
    {
        ScribeDefs.Look(ref Def!, "Def");
        ScribeValues.Look(ref Charges, "Charges");
        ScribeDeep.Look(ref Trigger!, "Trigger");
        Trigger ??= new MedicalTrigger();
    }
}

public class MedicalChest : IExposable
{
    public const int DefaultCapacity = 4;
    public const int DefaultCooldownInTicks = 180;

    public int Capacity = DefaultCapacity;
    private Pawn _pawn = null!;
    private List<MedicalChestSlot> _slots = [];

    public IReadOnlyList<MedicalChestSlot> Slots => _slots;

    public MedicalChest()
    {
    }

    public MedicalChest(Pawn pawn)
    {
        _pawn = pawn;
    }

    public bool TryArm(Item item, MedicalTrigger? trigger = null)
    {
        Prune();
        if (item == null || item.IsDestroyed || item.StackSize < 1 || !IsMedicalItem(item))
        {
            return false;
        }

        if (_slots.Count >= Capacity)
        {
            return false;
        }

        if (!TryConsumeFromInventory(item.ItemDef, 1))
        {
            return false;
        }

        _slots.Add(new MedicalChestSlot
        {
            Def = item.ItemDef,
            Charges = IsInfiniteUse(item.ItemDef) ? 0 : 1,
            Trigger = trigger ?? DefaultTriggerFor(item.ItemDef)
        });
        return true;
    }

    public bool TryInstall(ItemDef def, int charges, MedicalTrigger? trigger = null)
    {
        Prune();
        if (def == null || !IsMedicalItem(def) || _slots.Count >= Capacity)
        {
            return false;
        }

        _slots.Add(new MedicalChestSlot
        {
            Def = def,
            Charges = IsInfiniteUse(def) ? 0 : Math.Max(0, charges),
            Trigger = trigger ?? DefaultTriggerFor(def)
        });
        return true;
    }

    public bool AddCharge(MedicalChestSlot slot)
    {
        if (slot == null || !_slots.Contains(slot) || slot.IsInfinite)
        {
            return false;
        }

        if (!TryConsumeFromInventory(slot.Def, 1))
        {
            return false;
        }

        slot.Charges++;
        return true;
    }

    public bool RemoveCharge(MedicalChestSlot slot)
    {
        if (slot == null || !_slots.Contains(slot) || slot.IsInfinite || slot.Charges < 1)
        {
            return false;
        }

        slot.Charges--;
        ReturnToInventory(slot.Def, 1);
        return true;
    }

    public void LoadMax(MedicalChestSlot slot)
    {
        if (slot == null || !_slots.Contains(slot) || slot.IsInfinite || _pawn?.Inventory == null)
        {
            return;
        }

        var available = _pawn.Inventory.AmountOf(slot.Def);
        for (var i = 0; i < available; i++)
        {
            if (!AddCharge(slot))
            {
                break;
            }
        }
    }

    public void Remove(MedicalChestSlot slot)
    {
        if (slot == null || !_slots.Remove(slot))
        {
            return;
        }

        if (slot.IsInfinite)
        {
            ReturnToInventory(slot.Def, 1);
            return;
        }

        if (slot.Charges > 0)
        {
            ReturnToInventory(slot.Def, slot.Charges);
        }
    }

    public void Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _slots.Count || toIndex < 0 || toIndex >= _slots.Count || fromIndex == toIndex)
        {
            return;
        }

        var slot = _slots[fromIndex];
        _slots.RemoveAt(fromIndex);
        _slots.Insert(toIndex, slot);
    }

    public void Clear()
    {
        _slots.Clear();
    }

    public void Prune()
    {
        _slots.RemoveAll(s => s.Def == null);
    }

    public void ResetCooldowns()
    {
        foreach (var slot in _slots)
        {
            slot.NextReadyTick = 0;
        }
    }

    public static bool IsMedicalItem(Item item)
    {
        return item != null && IsMedicalItem(item.ItemDef);
    }

    public static bool IsMedicalItem(ItemDef? def)
    {
        return def != null && (def.ItemType == ItemType.Medical || def == Defs.Items.Cauterize);
    }

    public static bool IsInfiniteUse(ItemDef? def)
    {
        return def != null && (def.MedicinalProperties?.InfiniteUse == true || def == Defs.Items.Cauterize);
    }

    public static int CooldownInTicks(ItemDef? def)
    {
        var ticks = def?.MedicinalProperties?.CooldownInTicks ?? 0;
        return ticks > 0 ? ticks : DefaultCooldownInTicks;
    }

    public static MedicalTrigger DefaultTriggerFor(Item item)
    {
        return DefaultTriggerFor(item.ItemDef);
    }

    public static MedicalTrigger DefaultTriggerFor(ItemDef def)
    {
        if (def == Defs.Items.Cauterize)
        {
            return new MedicalTrigger
            {
                Type = MedicalTriggerType.PartSevered,
                TargetSelector = MedicalTargetSelector.SeveredOrUnsealedSocket
            };
        }

        return new MedicalTrigger
        {
            Type = MedicalTriggerType.Immediately,
            TargetSelector = MedicalTargetSelector.Auto
        };
    }

    public void ExposeData()
    {
        ScribeValues.Look(ref Capacity, "Capacity", DefaultCapacity);
        ScribeCollections.Look(ref _slots!, "Slots", LookMode.Deep);
        _slots ??= [];
        if (Capacity <= 0)
        {
            Capacity = DefaultCapacity;
        }
    }

    private bool TryConsumeFromInventory(ItemDef def, int amount)
    {
        if (_pawn?.Inventory == null || def == null || amount < 1)
        {
            return false;
        }

        if (_pawn.Inventory.AmountOf(def) < amount)
        {
            return false;
        }

        var remaining = amount;
        while (remaining > 0)
        {
            var taken = _pawn.Inventory.Take(def, remaining);
            if (taken == null)
            {
                return false;
            }

            remaining -= taken.StackSize;
            taken.Destroy();
        }

        return true;
    }

    private void ReturnToInventory(ItemDef def, int amount)
    {
        if (_pawn?.Inventory == null || def == null || amount < 1)
        {
            return;
        }

        var existing = _pawn.Inventory.FirstOrDefault(i => i.Def == def && !i.IsDestroyed);
        if (existing != null && existing.IsStackable)
        {
            existing.StackSize += amount;
            return;
        }

        _pawn.Inventory.TryAdd(_pawn.Context.Factory.CreateEntity<Item>(def, amount));
    }
}
