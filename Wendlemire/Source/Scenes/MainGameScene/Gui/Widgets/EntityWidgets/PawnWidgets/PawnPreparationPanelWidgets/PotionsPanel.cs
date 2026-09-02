using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class PotionsPanel : VerticalStackPanel, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly Grid _slots;
    private readonly Label _countLabel;
    private readonly PrepItemGrid _inventory;
    private readonly List<PotionTriggerEditor> _editors = [];
    private string _signature = "";

    public const int PanelHeight = 350;

    public PotionsPanel(BaseGui gui, Pawn pawn)
    {
        _gui = gui;
        _pawn = pawn;
        Spacing = 8;
        Padding = new Thickness(0);
        Height = PanelHeight;
        MinHeight = PanelHeight;
        MaxHeight = PanelHeight;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _countLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = new Color(160, 160, 160)
        };

        _inventory = new PrepItemGrid(
            gui,
            pawn.Inventory,
            item => item.ItemDef.ItemType == ItemType.Potion,
            TryEquipPotion,
            item => EquippedCount() >= _pawn.PotionCapacity
                ? "All potion slots full"
                : IsEquippedDef(item)
                    ? "Equipped"
                    : "Click to equip",
            IsEquippedDef,
            _ => EquippedCount() >= _pawn.PotionCapacity);
        Widgets.Add(new PotionInventoryCard(_inventory, _countLabel));

        _slots = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _slots.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        _slots.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        _slots.RowsProportions.Add(new Proportion(ProportionType.Part, 1));
        _slots.RowsProportions.Add(new Proportion(ProportionType.Part, 1));
        Widgets.Add(_slots);
        SetProportionType(_slots, ProportionType.Fill);
        Rebuild();
    }

    public void Update()
    {
        var signature = SlotSignature();
        if (signature != _signature)
        {
            Rebuild();
        }
        else
        {
            foreach (var editor in _editors)
            {
                editor.Update();
            }
        }

        _inventory.Update();
    }

    private int EquippedCount() => _pawn.Equipment.Potions.Count();

    private bool IsEquippedDef(Item item)
    {
        return _pawn.Equipment.Potions.Any(p => p.Def == item.Def);
    }

    private string SlotSignature()
    {
        return $"{_pawn.PotionCapacity}:" + string.Join(",", _pawn.Equipment.Potions.Select(p => p.Id));
    }

    private void TryEquipPotion(Item item)
    {
        foreach (var part in _pawn.Body.AllExternalParts)
        {
            var slot = part.EmptySlotFor(item);
            if (slot == null)
            {
                continue;
            }

            Item potion;
            if (item.StackSize > 1)
            {
                item.StackSize--;
                potion = Core.Context.Factory.CreateEntity<Item>(item.ItemDef, 1);
            }
            else
            {
                potion = item;
                item.EjectFromContainer();
            }

            var swapped = _pawn.Equipment.TryEquip(part, slot.Value, potion);
            if (swapped != null)
            {
                _pawn.Inventory.TryAdd(swapped);
            }

            Rebuild();
            return;
        }

        _gui.ViewEntity(item);
    }

    private void UnequipPotion(Item potion)
    {
        var item = _pawn.Equipment.UnEquip(potion);
        if (item != null)
        {
            _pawn.Inventory.TryAdd(item);
        }

        Rebuild();
    }

    private void Rebuild()
    {
        _slots.Widgets.Clear();
        _editors.Clear();
        _signature = SlotSignature();

        var equipped = _pawn.Equipment.Potions.ToList();
        var capacity = Math.Max(_pawn.PotionCapacity, 1);
        var unused = Math.Max(0, capacity - equipped.Count);
        _countLabel.Text = unused > 0
            ? $"{equipped.Count}/{capacity} equipped — {unused} unused"
            : $"{equipped.Count}/{capacity} equipped";
        var lockTooltip = LockedSlotTooltip();
        for (var i = 0; i < PotionSlots.MaxSlots; i++)
        {
            Widget card = i < equipped.Count
                ? CreateCard(equipped[i])
                : i < capacity
                    ? PotionSlotChrome.Empty()
                    : LockedSlotChrome.Card(lockTooltip.title, lockTooltip.description, 72);
            _slots.Widgets.Add(card);
            Grid.SetColumn(card, i % 2);
            Grid.SetRow(card, i / 2);
        }
    }

    private (string title, string? description) LockedSlotTooltip()
    {
        return SlotUnlockTooltip.For(
            _pawn.Context?.Achievements,
            _pawn.Context?.Achievements != null
                ? PotionSlots.NextLockedSlotAchievement(_pawn.Context.Achievements)
                : null,
            "Complete potion achievements to unlock this slot.");
    }

    private PotionTriggerEditor CreateCard(Item potion)
    {
        var editor = new PotionTriggerEditor(_gui, potion, () => UnequipPotion(potion));
        _editors.Add(editor);
        return editor;
    }

    private sealed class PotionInventoryCard : PrepCard
    {
        public PotionInventoryCard(Widget inventory, Widget count) : base("Potions")
        {
            UseFixedBody();
            VerticalAlignment = VerticalAlignment.Top;
            Body.Widgets.Add(inventory);
            Body.Widgets.Add(count);
        }
    }
}

internal static class PotionSlotChrome
{
    public static void Apply(Panel card)
    {
        card.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        card.Padding = new Thickness(8);
        card.HorizontalAlignment = HorizontalAlignment.Stretch;
        card.VerticalAlignment = VerticalAlignment.Stretch;
        card.ClipToBounds = true;
    }

    public static Panel Empty()
    {
        var card = new Panel();
        Apply(card);
        card.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Empty",
            TextColor = new Color(120, 120, 120),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        return card;
    }
}

internal sealed class PotionTriggerEditor : Panel, IUpdatable
{
    private readonly Item _potion;
    private readonly OptionDropdown<PotionTriggerType> _typeDropdown;
    private readonly ThresholdSlider _slider;
    private readonly Label _summary;
    private readonly IReadOnlyList<PotionTriggerType> _allowed;

    public PotionTriggerEditor(BaseGui gui, Item potion, Action onUnequip)
    {
        _potion = potion;
        PotionSlotChrome.Apply(this);

        var body = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Widgets.Add(body);

        potion.PotionTrigger ??= potion.ItemDef.PotionProperties?.DefaultTrigger?.Clone()
                                 ?? new PotionTrigger { Type = PotionTriggerType.Immediately };

        _allowed = potion.ItemDef.PotionProperties?.GetAllowedTriggerTypes()
                   ?? Enum.GetValues<PotionTriggerType>();

        body.Widgets.Add(CreateHeader(gui, potion, onUnequip));

        _typeDropdown = new OptionDropdown<PotionTriggerType>(
            gui.Desktop,
            _allowed,
            TriggerLabels.For,
            potion.PotionTrigger.Type,
            ApplyType)
        {
            MinWidth = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        body.Widgets.Add(_typeDropdown);

        _slider = new ThresholdSlider(
            SliderCaption(potion.PotionTrigger.Type),
            SliderMode(potion.PotionTrigger.Type),
            CurrentSliderValue(potion.PotionTrigger));
        _slider.ValueChanged += ApplyValue;
        body.Widgets.Add(_slider);

        _summary = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = new Color(200, 180, 140),
            Wrap = true
        };
        body.Widgets.Add(_summary);

        RefreshControls();
    }

    public void Update()
    {
        _typeDropdown.Update();
    }

    private static Widget CreateHeader(BaseGui gui, Item potion, Action onUnequip)
    {
        var name = new Label(BaseContent.Styles.Label.Small)
        {
            Text = potion.Label,
            VerticalAlignment = VerticalAlignment.Center
        };
        name.TouchDown += (_, _) => gui.ViewEntity(potion);

        var remove = new CursorButton(BaseContent.Styles.Button.Small)
        {
            Width = 22,
            Height = 22,
            Padding = new Thickness(3),
            Content = new Image
            {
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Close],
                Width = 12,
                Height = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        remove.Click += (_, _) => onUnequip();

        var icon = new Image
        {
            Background = potion.GetIconImage(),
            Width = BaseContent.IconSizes.Small,
            Height = BaseContent.IconSizes.Small,
            VerticalAlignment = VerticalAlignment.Center
        };
        icon.TouchDown += (_, _) => gui.ViewEntity(potion);

        var header = new Grid
        {
            ColumnSpacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        header.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        header.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        header.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        header.Widgets.Add(icon);
        header.Widgets.Add(name);
        header.Widgets.Add(remove);
        Grid.SetColumn(icon, 0);
        Grid.SetColumn(name, 1);
        Grid.SetColumn(remove, 2);
        return header;
    }

    private void ApplyType(PotionTriggerType type)
    {
        if (_potion.PotionTrigger == null)
        {
            return;
        }

        _potion.PotionTrigger.Type = type;
        RefreshControls();
    }

    private void ApplyValue(float value)
    {
        if (_potion.PotionTrigger == null)
        {
            return;
        }

        switch (_potion.PotionTrigger.Type)
        {
            case PotionTriggerType.AfterSeconds:
                _potion.PotionTrigger.AfterSeconds = Math.Clamp(
                    value, ThresholdSlider.MinSeconds, ThresholdSlider.MaxSeconds);
                break;
            case PotionTriggerType.SelfBloodBelow:
            case PotionTriggerType.EnemyBloodBelow:
                _potion.PotionTrigger.Threshold = Math.Clamp(
                    value, ThresholdSlider.MinBlood, ThresholdSlider.MaxBlood);
                break;
            case PotionTriggerType.SelfPartsDamaged:
                _potion.PotionTrigger.Threshold = Math.Clamp(value, 0, 1);
                break;
        }

        _summary.Text = TriggerLabels.Summarize(_potion.PotionTrigger);
    }

    private void RefreshControls()
    {
        var trigger = _potion.PotionTrigger;
        if (trigger == null)
        {
            return;
        }

        var showSlider = trigger.Type != PotionTriggerType.Immediately;
        _slider.Visible = showSlider;
        if (showSlider)
        {
            _slider.SetCaption(SliderCaption(trigger.Type));
            _slider.Configure(SliderMode(trigger.Type), CurrentSliderValue(trigger));
            if (trigger.Type is PotionTriggerType.AfterSeconds
                or PotionTriggerType.SelfBloodBelow
                or PotionTriggerType.EnemyBloodBelow)
            {
                ApplyValue(_slider.StoredValue);
            }
        }

        _summary.Text = TriggerLabels.Summarize(trigger);
    }

    private static ThresholdSliderMode SliderMode(PotionTriggerType type)
    {
        return type switch
        {
            PotionTriggerType.AfterSeconds => ThresholdSliderMode.Seconds,
            PotionTriggerType.SelfBloodBelow or PotionTriggerType.EnemyBloodBelow => ThresholdSliderMode.Blood,
            _ => ThresholdSliderMode.Percent
        };
    }

    private static string SliderCaption(PotionTriggerType type)
    {
        return type switch
        {
            PotionTriggerType.AfterSeconds => "Delay",
            PotionTriggerType.SelfPartsDamaged => "Parts",
            _ => "Blood"
        };
    }

    private static float CurrentSliderValue(PotionTrigger trigger)
    {
        return trigger.Type == PotionTriggerType.AfterSeconds
            ? trigger.AfterSeconds
            : trigger.Threshold;
    }
}
