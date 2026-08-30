namespace Wendlewind.Sim.Entities.Items.Medicinals;

public class MedicalChestSlot : IExposable
{
    public Item Item = null!;
    public MedicalTrigger Trigger = new();

    public void ExposeData()
    {
        ScribeReferences.Look(ref Item!, "Item");
        ScribeDeep.Look(ref Trigger!, "Trigger");
        Trigger ??= new MedicalTrigger();
    }
}

public class MedicalChest : IExposable
{
    public const int DefaultCapacity = 4;

    public int Capacity = DefaultCapacity;
    private List<MedicalChestSlot> _slots = [];

    public IReadOnlyList<MedicalChestSlot> Slots => _slots;

    public MedicalChest()
    {
    }

    public MedicalChest(Pawn pawn)
    {
    }

    public bool TryAdd(Item item, MedicalTrigger? trigger = null)
    {
        Prune();
        if (item.IsDestroyed || item.StackSize < 1)
        {
            return false;
        }

        if (!IsMedicalItem(item))
        {
            return false;
        }

        if (_slots.Count >= Capacity)
        {
            return false;
        }

        if (_slots.Any(s => s.Item == item))
        {
            return false;
        }

        _slots.Add(new MedicalChestSlot
        {
            Item = item,
            Trigger = trigger ?? DefaultTriggerFor(item)
        });
        return true;
    }

    public void Remove(MedicalChestSlot slot)
    {
        _slots.Remove(slot);
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

    public void Prune()
    {
        _slots.RemoveAll(s => s.Item == null || s.Item.IsDestroyed || s.Item.StackSize < 1);
    }

    public static bool IsMedicalItem(Item item)
    {
        return item.ItemDef.ItemType == ItemType.Medical || item.Def == Defs.Items.Cauterize;
    }

    public static MedicalTrigger DefaultTriggerFor(Item item)
    {
        if (item.Def == Defs.Items.Cauterize)
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
}
