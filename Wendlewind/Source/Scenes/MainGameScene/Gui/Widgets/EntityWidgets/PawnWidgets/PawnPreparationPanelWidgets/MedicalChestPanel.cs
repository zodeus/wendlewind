using Image = Myra.Graphics2D.UI.Image;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class MedicalChestPanel : PrepCard, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly VerticalStackPanel _slots;
    private readonly PrepItemGrid _inventory;
    private readonly List<MedicalTriggerEditor> _editors = [];

    public MedicalChestPanel(BaseGui gui, Pawn pawn) : base("Medical")
    {
        _gui = gui;
        _pawn = pawn;
        _slots = new VerticalStackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Stretch };
        Body.Widgets.Add(_slots);

        _inventory = new PrepItemGrid(
            gui,
            pawn.Inventory,
            MedicalChest.IsMedicalItem,
            TryArm,
            item => IsArmed(item)
                ? "Armed"
                : _pawn.MedicalChest.Slots.Count >= _pawn.MedicalChest.Capacity
                    ? "Chest full"
                    : "Click to arm",
            IsArmed,
            item => !IsArmed(item) && _pawn.MedicalChest.Slots.Count >= _pawn.MedicalChest.Capacity);
        SetInventory(_inventory);
        Rebuild();
    }

    public void Update()
    {
        _pawn.MedicalChest.Prune();
        if (_editors.Count != _pawn.MedicalChest.Slots.Count)
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
        return _pawn.MedicalChest.Slots.Any(s => s.Item == item);
    }

    private void TryArm(Item item)
    {
        if (IsArmed(item))
        {
            _gui.ViewEntity(item);
            return;
        }

        if (_pawn.MedicalChest.TryAdd(item))
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        _slots.Widgets.Clear();
        _editors.Clear();
        foreach (var slot in _pawn.MedicalChest.Slots)
        {
            var editor = new MedicalTriggerEditor(_gui, _pawn, slot, Rebuild);
            _editors.Add(editor);
            _slots.Widgets.Add(editor);
        }

        if (_editors.Count == 0)
        {
            _slots.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Click a medical item above to arm it (0/{_pawn.MedicalChest.Capacity})",
                TextColor = new Color(140, 140, 140)
            });
        }
    }
}

internal sealed class MedicalTriggerEditor : VerticalStackPanel, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly MedicalChestSlot _slot;
    private readonly OptionDropdown<MedicalTriggerType> _typeDropdown;
    private readonly OptionDropdown<MedicalTargetSelector> _targetDropdown;
    private readonly OptionDropdown<BodyPart>? _partDropdown;
    private readonly ThresholdSlider _slider;
    private readonly Label _summary;

    public MedicalTriggerEditor(BaseGui gui, Pawn pawn, MedicalChestSlot slot, Action onRemoved)
    {
        _pawn = pawn;
        _slot = slot;
        Spacing = 4;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Padding = new Thickness(4);
        Background = new SolidBrush(new Color(20, 18, 16, 80));

        Widgets.Add(CreateHeader(gui, slot, onRemoved));

        var controlRow = new HorizontalStackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        _typeDropdown = new OptionDropdown<MedicalTriggerType>(
            gui.Desktop,
            Enum.GetValues<MedicalTriggerType>(),
            TriggerLabels.For,
            slot.Trigger.Type,
            ApplyType);
        _targetDropdown = new OptionDropdown<MedicalTargetSelector>(
            gui.Desktop,
            Enum.GetValues<MedicalTargetSelector>(),
            TriggerLabels.For,
            slot.Trigger.TargetSelector,
            ApplyTarget);
        controlRow.Widgets.Add(_typeDropdown);
        controlRow.Widgets.Add(_targetDropdown);

        var parts = pawn.Body.AllExternalParts.ToList();
        var initialPart = CurrentPart() ?? parts.FirstOrDefault();
        if (initialPart != null)
        {
            _partDropdown = new OptionDropdown<BodyPart>(
                gui.Desktop,
                parts,
                part => part.Label,
                initialPart,
                ApplyPart);
            controlRow.Widgets.Add(_partDropdown);
        }

        Widgets.Add(controlRow);

        _slider = new ThresholdSlider(
            SliderCaption(slot.Trigger.Type),
            SliderMode(slot.Trigger.Type),
            CurrentSliderValue(slot.Trigger));
        _slider.ValueChanged += ApplyValue;
        Widgets.Add(_slider);

        _summary = new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = new Color(200, 180, 140),
            Wrap = true
        };
        Widgets.Add(_summary);

        RefreshControls();
    }

    public void Update()
    {
        _typeDropdown.Update();
        _targetDropdown.Update();
        _partDropdown?.Update();
    }

    private Widget CreateHeader(BaseGui gui, MedicalChestSlot slot, Action onRemoved)
    {
        var name = new Label(BaseContent.Styles.Label.Small)
        {
            Text = slot.Item.Label,
            VerticalAlignment = VerticalAlignment.Center
        };

        var remove = new CursorButton(BaseContent.Styles.Button.Small)
        {
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Remove" }
        };
        remove.Click += (_, _) =>
        {
            _pawn.MedicalChest.Remove(slot);
            onRemoved();
        };

        var icon = new Image
        {
            Background = new TextureRegion(slot.Item.GetIcon()),
            Width = BaseContent.IconSizes.Small,
            Height = BaseContent.IconSizes.Small,
            VerticalAlignment = VerticalAlignment.Center
        };
        icon.TouchDown += (_, _) => gui.ViewEntity(slot.Item);
        name.TouchDown += (_, _) => gui.ViewEntity(slot.Item);

        var row = new HorizontalStackPanel { Spacing = 6 };
        row.Widgets.Add(icon);
        row.Widgets.Add(name);
        row.Widgets.Add(remove);
        return row;
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
            var part = _pawn.Body.AllExternalParts.FirstOrDefault();
            if (part != null)
            {
                _slot.Trigger.TargetPartKey = part.InternalLabel;
                _partDropdown?.SetCurrent(part);
            }
        }

        RefreshControls();
    }

    private void ApplyPart(BodyPart part)
    {
        _slot.Trigger.TargetPartKey = part.InternalLabel;
        RefreshSummary();
    }

    private void ApplyValue(float value)
    {
        switch (_slot.Trigger.Type)
        {
            case MedicalTriggerType.AfterSeconds:
                _slot.Trigger.AfterSeconds = Math.Max(0, value);
                break;
            case MedicalTriggerType.SelfBloodBelow:
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
        var showValue = _slot.Trigger.Type is MedicalTriggerType.AfterSeconds
            or MedicalTriggerType.SelfBloodBelow
            or MedicalTriggerType.SelfPartsDamaged
            or MedicalTriggerType.PartBelowHealth;
        _slider.Visible = showValue;
        if (showValue)
        {
            _slider.SetCaption(SliderCaption(_slot.Trigger.Type));
            _slider.Configure(SliderMode(_slot.Trigger.Type), CurrentSliderValue(_slot.Trigger));
        }

        if (_partDropdown != null)
        {
            _partDropdown.Visible = _slot.Trigger.TargetSelector == MedicalTargetSelector.SpecificPart;
            if (_partDropdown.Visible)
            {
                var parts = _pawn.Body.AllExternalParts.ToList();
                var current = CurrentPart() ?? parts.FirstOrDefault();
                if (current != null)
                {
                    _partDropdown.SetItems(parts, current);
                }
            }
        }

        RefreshSummary();
    }

    private void RefreshSummary()
    {
        _summary.Text = TriggerLabels.Summarize(_slot.Trigger, CurrentPart()?.Label);
    }

    private BodyPart? CurrentPart()
    {
        return _pawn.Body.FindPartByKey(_slot.Trigger.TargetPartKey);
    }

    private static ThresholdSliderMode SliderMode(MedicalTriggerType type)
    {
        return type == MedicalTriggerType.AfterSeconds
            ? ThresholdSliderMode.Seconds
            : ThresholdSliderMode.Percent;
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
