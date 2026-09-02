using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class MedicalChestPanel : VerticalStackPanel, IUpdatable
{
    private const int CardsPerRow = 3;

    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly Grid _slots;
    private readonly Label _countLabel;
    private readonly PrepItemGrid _inventory;
    private readonly List<MedicalSlotCard> _editors = [];
    private string _signature = "";

    public MedicalChestPanel(BaseGui gui, Pawn pawn)
    {
        _gui = gui;
        _pawn = pawn;
        Spacing = 8;
        Padding = new Thickness(0);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _countLabel = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = new Color(160, 160, 160)
        };

        _inventory = new PrepItemGrid(
            gui,
            pawn.Inventory,
            MedicalChest.IsMedicalItem,
            TryArm,
            item => _pawn.MedicalChest.Slots.Count >= _pawn.MedicalChest.Capacity
                ? "Chest full"
                : IsArmed(item)
                    ? "Click to arm another slot"
                    : "Click to arm",
            IsArmed,
            _ => _pawn.MedicalChest.Slots.Count >= _pawn.MedicalChest.Capacity,
            pagedRow: true,
            centerRow: true,
            rowCells: MedicalChest.MaxSlots);

        Widgets.Add(new MedicalInventoryCard(_inventory, _countLabel));

        _slots = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 8,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        for (var i = 0; i < CardsPerRow; i++)
        {
            _slots.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        }
        Widgets.Add(_slots);
        SetProportionType(_slots, ProportionType.Fill);
        Rebuild();
    }

    public void Update()
    {
        _pawn.MedicalChest.Prune();
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

    private bool IsArmed(Item item)
    {
        return _pawn.MedicalChest.Slots.Any(s => s.Def == item.Def);
    }

    private void TryArm(Item item)
    {
        if (_pawn.MedicalChest.TryArm(item))
        {
            Rebuild();
        }
    }

    private string SlotSignature()
    {
        var slots = _pawn.MedicalChest.Slots;
        return $"{_pawn.MedicalChest.Capacity}:{slots.Count}:" +
               string.Join(",", slots.Select(s => s.Def.Moniker));
    }

    private void Rebuild()
    {
        _slots.Widgets.Clear();
        _slots.RowsProportions.Clear();
        _editors.Clear();
        _signature = SlotSignature();

        var slots = _pawn.MedicalChest.Slots;
        var capacity = Math.Max(_pawn.MedicalChest.Capacity, 1);
        var unused = Math.Max(0, capacity - slots.Count);
        var maxSlots = MedicalChest.MaxSlots;
        _countLabel.Text = unused > 0
            ? $"{slots.Count}/{capacity} armed — {unused} unused"
            : $"{slots.Count}/{capacity} armed";
        _countLabel.TextColor = unused > 0
            ? MedicalSlotChrome.UnusedAccent
            : new Color(160, 160, 160);

        var rows = (maxSlots + CardsPerRow - 1) / CardsPerRow;
        for (var r = 0; r < rows; r++)
        {
            _slots.RowsProportions.Add(new Proportion(ProportionType.Part, 1));
        }

        var lockTooltip = LockedSlotTooltip();
        for (var i = 0; i < maxSlots; i++)
        {
            Widget card = i < slots.Count
                ? CreateArmedCard(slots[i])
                : i < capacity
                    ? MedicalSlotChrome.Empty()
                    : MedicalSlotChrome.Locked(lockTooltip.title, lockTooltip.description);
            _slots.Widgets.Add(card);
            Grid.SetColumn(card, i % CardsPerRow);
            Grid.SetRow(card, i / CardsPerRow);
        }
    }

    private (string title, string? description) LockedSlotTooltip()
    {
        return SlotUnlockTooltip.For(
            _pawn.Context?.Achievements,
            _pawn.Context?.Achievements != null
                ? MedicalChest.NextLockedSlotAchievement(_pawn.Context.Achievements)
                : null,
            "Complete medical achievements to unlock this slot.");
    }

    private MedicalSlotCard CreateArmedCard(MedicalChestSlot slot)
    {
        var editor = new MedicalSlotCard(_gui, _pawn, slot, Rebuild);
        _editors.Add(editor);
        return editor;
    }

    private sealed class MedicalInventoryCard : PrepCard
    {
        public MedicalInventoryCard(Widget inventory, Widget count) : base("Medical")
        {
            UseFixedBody();
            VerticalAlignment = VerticalAlignment.Top;
            Body.Widgets.Add(inventory);
            Body.Widgets.Add(count);
        }
    }
}

internal static class SlotUnlockTooltip
{
    public static (string title, string? description) For(
        AchievementTracker? tracker,
        AchievementDef? next,
        string fallback)
    {
        if (tracker == null || next == null)
        {
            return ("Locked", fallback);
        }

        var progress = tracker.GetProgress(next);
        var remaining = Math.Max(0, next.TargetValue - (progress?.CurrentValue ?? 0));
        var description = remaining > 0
            ? $"{next.Description} ({remaining:0} remaining). {next.BenifitDescription}."
            : next.BenifitDescription;
        return (next.Label, description);
    }
}

internal static class LockedSlotChrome
{
    private static Texture2D? _icon;
    private static bool _tried;

    public static Image Icon(int size)
    {
        var image = new Image
        {
            Width = size,
            Height = size,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.25f
        };
        var texture = Texture();
        if (texture != null)
        {
            image.Background = new TextureRegion(texture);
        }

        return image;
    }

    public static Panel Card(string title, string? description, int iconSize = 96)
    {
        var card = new Panel();
        MedicalSlotChrome.Apply(card);
        card.Widgets.Add(Icon(iconSize));
        card.WithTooltip(title, description);
        return card;
    }

    public static Panel Slot(string title, string? description)
    {
        var icon = Icon(PrepSlots.Size - PrepSlots.Pad * 2);
        icon.WithTooltip(title, description);
        return PrepSlots.Frame(icon);
    }

    private static Texture2D? Texture()
    {
        if (_tried)
        {
            return _icon;
        }

        _tried = true;
        try
        {
            _icon = EntityVisuals.LoadPremultiplied("UI/Icons/icon-lock");
        }
        catch
        {
            _icon = null;
        }

        return _icon;
    }
}

internal static class MedicalSlotChrome
{
    public static readonly Color UnusedAccent = new(220, 160, 80);

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
        card.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];

        var body = new VerticalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var icon = DefRepository<LootBoxDef>.GetByMoniker("MedicinalChest", raiseError: false)?.GetIcon();
        if (icon != null)
        {
            body.Widgets.Add(new Image
            {
                Background = new ColoredIcon(new TextureRegion(icon), UnusedAccent),
                Width = 96,
                Height = 96,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        body.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "Unused",
            TextColor = UnusedAccent,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        body.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Arm a medical item",
            TextColor = new Color(200, 150, 90),
            HorizontalAlignment = HorizontalAlignment.Center,
            Wrap = true
        });
        card.Widgets.Add(body);
        card.WithTooltip("Unused medical slot", "This slot is empty and will not heal in the fight.");
        return card;
    }

    public static Panel Locked(string title, string? description)
    {
        return LockedSlotChrome.Card(title, description);
    }
}

internal sealed class MedicalSlotCard : Panel, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly MedicalChestSlot _slot;
    private readonly OptionDropdown<MedicalTriggerType> _typeDropdown;
    private readonly OptionDropdown<MedicalTargetSelector> _targetDropdown;
    private readonly OptionDropdown<BodyPart>? _partDropdown;
    private readonly ThresholdSlider _slider;
    private readonly Label _summary;
    private readonly ChargeStepper _charges;
    private readonly Label _wholeBody;

    public MedicalSlotCard(BaseGui gui, Pawn pawn, MedicalChestSlot slot, Action onRemoved)
    {
        _pawn = pawn;
        _slot = slot;
        MedicalSlotChrome.Apply(this);

        var body = new VerticalStackPanel
        {
            Spacing = 5,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Widgets.Add(body);

        body.Widgets.Add(CreateHeader(gui, slot, onRemoved));

        _charges = new ChargeStepper(compact: true);
        _charges.Decrement += () => _pawn.MedicalChest.RemoveCharge(_slot);
        _charges.Increment += () => _pawn.MedicalChest.AddCharge(_slot);
        _charges.LoadMax += () => _pawn.MedicalChest.LoadMax(_slot);
        body.Widgets.Add(_charges);

        MedicalChest.Sanitize(slot);

        _typeDropdown = CompactDropdown(
            gui.Desktop,
            AllowedTypes(),
            TriggerLabels.For,
            slot.Trigger.Type,
            ApplyType);
        body.Widgets.Add(_typeDropdown);

        _targetDropdown = CompactDropdown(
            gui.Desktop,
            AllowedTargets(),
            TriggerLabels.For,
            slot.Trigger.TargetSelector,
            ApplyTarget);

        var parts = SelectableParts();
        var initialPart = CurrentPart() ?? parts.FirstOrDefault();
        if (initialPart != null)
        {
            _partDropdown = CompactDropdown(
                gui.Desktop,
                parts,
                MedicalTrigger.GroupLabel,
                initialPart,
                ApplyPart,
                maxItemsPerColumn: 10,
                MedicalTrigger.UsesRegionGroups(slot.Def.MedicinalProperties)
                    ? MedicalTrigger.RegionGroupLabel
                    : null);
        }

        _wholeBody = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Whole body",
            TextColor = new Color(160, 160, 160),
            Visible = false
        };

        var targetRow = new Grid
        {
            ColumnSpacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        targetRow.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        targetRow.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        targetRow.Widgets.Add(_targetDropdown);
        Grid.SetColumn(_targetDropdown, 0);
        Grid.SetColumnSpan(_targetDropdown, 2);
        if (_partDropdown != null)
        {
            targetRow.Widgets.Add(_partDropdown);
            Grid.SetColumn(_partDropdown, 1);
        }

        body.Widgets.Add(targetRow);
        body.Widgets.Add(_wholeBody);

        _slider = new ThresholdSlider(
            SliderCaption(slot.Trigger.Type),
            SliderMode(slot.Trigger.Type),
            CurrentSliderValue(slot.Trigger));
        _slider.ValueChanged += ApplyValue;
        body.Widgets.Add(_slider);

        _summary = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = new Color(200, 180, 140),
            Wrap = true
        };
        body.Widgets.Add(_summary);

        RefreshControls();
        RefreshCharges();
    }

    public void Update()
    {
        _typeDropdown.Update();
        _targetDropdown.Update();
        _partDropdown?.Update();
        RefreshCharges();
    }

    private void RefreshCharges()
    {
        if (_slot.IsInfinite)
        {
            _charges.SetInfinite();
            return;
        }

        _charges.Set(
            _slot.Charges,
            _slot.Charges > 0,
            _pawn.Inventory.AmountOf(_slot.Def) > 0);
    }

    private static OptionDropdown<T> CompactDropdown<T>(
        Desktop desktop,
        IReadOnlyList<T> items,
        Func<T, string> labels,
        T current,
        Action<T> onSelect,
        int maxItemsPerColumn = 0,
        Func<T, string>? groupSelector = null)
    {
        return new OptionDropdown<T>(desktop, items, labels, current, onSelect, maxItemsPerColumn, groupSelector)
        {
            MinWidth = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    private Widget CreateHeader(BaseGui gui, MedicalChestSlot slot, Action onRemoved)
    {
        var name = new Label(BaseContent.Styles.Label.Small)
        {
            Text = slot.Def.Label,
            VerticalAlignment = VerticalAlignment.Center
        };

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
        remove.Click += (_, _) =>
        {
            _pawn.MedicalChest.Remove(slot);
            onRemoved();
        };

        var icon = new Image
        {
            Background = slot.Def.GetIconImage(),
            Width = BaseContent.IconSizes.Small,
            Height = BaseContent.IconSizes.Small,
            VerticalAlignment = VerticalAlignment.Center
        };
        icon.TouchDown += (_, _) => ViewSlot(gui, slot);
        name.TouchDown += (_, _) => ViewSlot(gui, slot);

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

    private void ViewSlot(BaseGui gui, MedicalChestSlot slot)
    {
        var live = _pawn.Inventory.FirstOrDefault(i => i.Def == slot.Def && !i.IsDestroyed);
        if (live != null)
        {
            gui.ViewEntity(live);
            return;
        }

        gui.ViewEntity(_pawn.Context.Factory.CreateEntity<Item>(slot.Def, 1));
    }

    private IReadOnlyList<MedicalTriggerType> AllowedTypes()
    {
        return _slot.Def.MedicinalProperties?.GetAllowedTriggerTypes()
               ?? Enum.GetValues<MedicalTriggerType>();
    }

    private IReadOnlyList<MedicalTargetSelector> AllowedTargets()
    {
        return _slot.Def.MedicinalProperties?.GetAllowedTargetSelectors()
               ?? Enum.GetValues<MedicalTargetSelector>();
    }

    private bool HidesTargetSelector()
    {
        return _slot.Def.MedicinalProperties?.ApplyMode == MedicalApplyMode.Self;
    }

    private void ApplyType(MedicalTriggerType type)
    {
        _slot.Trigger.Type = type;
        RefreshControls();
    }

    private void ApplyTarget(MedicalTargetSelector selector)
    {
        _slot.Trigger.TargetSelector = selector;
        if (selector == MedicalTargetSelector.SpecificPart && string.IsNullOrEmpty(_slot.Trigger.TargetPartKey))
        {
            var part = SelectableParts().FirstOrDefault();
            if (part != null)
            {
                _slot.Trigger.TargetPartKey = MedicalTrigger.GroupKey(part);
                _partDropdown?.SetCurrent(part);
            }
        }

        RefreshControls();
    }

    private void ApplyPart(BodyPart part)
    {
        _slot.Trigger.TargetPartKey = MedicalTrigger.GroupKey(part);
        RefreshSummary();
    }

    private void ApplyValue(float value)
    {
        switch (_slot.Trigger.Type)
        {
            case MedicalTriggerType.AfterSeconds:
                _slot.Trigger.AfterSeconds = Math.Clamp(
                    value, ThresholdSlider.MinSeconds, ThresholdSlider.MaxSeconds);
                break;
            case MedicalTriggerType.SelfBloodBelow:
                _slot.Trigger.Threshold = Math.Clamp(
                    value, ThresholdSlider.MinBlood, ThresholdSlider.MaxBlood);
                break;
            case MedicalTriggerType.SelfPartsDamaged:
                _slot.Trigger.Threshold = Math.Clamp(value, 0, 1);
                break;
            case MedicalTriggerType.PartBelowHealth:
                _slot.Trigger.HealthThreshold = Math.Clamp(value, 0, 1);
                break;
        }

        RefreshSummary();
    }

    private void RefreshControls()
    {
        _typeDropdown.SetItems(AllowedTypes(), _slot.Trigger.Type);
        _targetDropdown.SetItems(AllowedTargets(), _slot.Trigger.TargetSelector);
        var hideTarget = HidesTargetSelector();
        _targetDropdown.Visible = !hideTarget;
        _wholeBody.Visible = hideTarget;

        var showValue = _slot.Trigger.Type is MedicalTriggerType.AfterSeconds
            or MedicalTriggerType.SelfBloodBelow
            or MedicalTriggerType.SelfPartsDamaged
            or MedicalTriggerType.PartBelowHealth;
        _slider.Visible = showValue;
        if (showValue)
        {
            _slider.SetCaption(SliderCaption(_slot.Trigger.Type));
            _slider.Configure(SliderMode(_slot.Trigger.Type), CurrentSliderValue(_slot.Trigger));
            if (_slot.Trigger.Type is MedicalTriggerType.AfterSeconds
                or MedicalTriggerType.SelfBloodBelow)
            {
                ApplyValue(_slider.StoredValue);
            }
        }

        if (_partDropdown != null)
        {
            var showPart = !HidesTargetSelector()
                           && _slot.Trigger.TargetSelector == MedicalTargetSelector.SpecificPart;
            _partDropdown.Visible = showPart;
            Grid.SetColumnSpan(_targetDropdown, showPart ? 1 : 2);
            if (showPart)
            {
                var parts = SelectableParts();
                var current = CurrentPart() ?? parts.FirstOrDefault();
                if (current != null)
                {
                    _slot.Trigger.TargetPartKey = MedicalTrigger.GroupKey(current);
                    _partDropdown.SetItems(parts, current);
                }
            }
        }

        RefreshSummary();
    }

    private void RefreshSummary()
    {
        _summary.Text = TriggerLabels.Summarize(_slot.Trigger, CurrentPart() is { } part
            ? MedicalTrigger.GroupLabel(part)
            : null);
    }

    private List<BodyPart> SelectableParts()
    {
        return MedicalTrigger.ListSelectableParts(_pawn, _slot.Def.MedicinalProperties);
    }

    private BodyPart? CurrentPart()
    {
        var parts = SelectableParts();
        var key = _slot.Trigger.TargetPartKey;
        var match = parts.FirstOrDefault(p => MedicalTrigger.GroupKey(p) == key);
        if (match != null)
        {
            return match;
        }

        var resolved = MedicalTrigger.ResolveTargetParts(_pawn, key).FirstOrDefault();
        if (resolved != null)
        {
            return parts.FirstOrDefault(p => p.Type == resolved.Type) ?? parts.FirstOrDefault();
        }

        return parts.FirstOrDefault();
    }

    private static ThresholdSliderMode SliderMode(MedicalTriggerType type)
    {
        return type switch
        {
            MedicalTriggerType.AfterSeconds => ThresholdSliderMode.Seconds,
            MedicalTriggerType.SelfBloodBelow => ThresholdSliderMode.Blood,
            _ => ThresholdSliderMode.Percent
        };
    }

    private static string SliderCaption(MedicalTriggerType type)
    {
        return type switch
        {
            MedicalTriggerType.AfterSeconds => "Delay",
            MedicalTriggerType.SelfPartsDamaged => "Parts",
            MedicalTriggerType.PartBelowHealth => "Health",
            _ => "Blood"
        };
    }

    private static float CurrentSliderValue(MedicalTrigger trigger)
    {
        return trigger.Type switch
        {
            MedicalTriggerType.AfterSeconds => trigger.AfterSeconds,
            MedicalTriggerType.PartBelowHealth => trigger.HealthThreshold,
            _ => trigger.Threshold
        };
    }
}
