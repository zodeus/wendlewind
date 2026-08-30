using Image = Myra.Graphics2D.UI.Image;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class PotionsPanel : PrepCard, IUpdatable
{
    private readonly BaseGui _gui;
    private readonly Pawn _pawn;
    private readonly VerticalStackPanel _editors;
    private readonly PrepItemGrid _inventory;
    private readonly List<PotionTriggerEditor> _editorList = [];

    public PotionsPanel(BaseGui gui, Pawn pawn) : base("Potions")
    {
        _gui = gui;
        _pawn = pawn;
        _editors = new VerticalStackPanel { Spacing = 6 };
        Body.Widgets.Add(_editors);

        _inventory = new PrepItemGrid(
            gui,
            pawn.Inventory,
            item => item.ItemDef.ItemType == ItemType.Potion,
            TryEquipPotion,
            item => IsEquippedDef(item) ? "Equipped" : "Click to equip",
            IsEquippedDef);
        SetInventory(_inventory);
        RefreshEditors();
    }

    public void Update()
    {
        var equipped = _pawn.Equipment.Potions.ToList();
        if (_editorList.Count != equipped.Count)
        {
            RefreshEditors();
        }
        else
        {
            foreach (var editor in _editorList)
            {
                editor.Update();
            }
        }

        _inventory.Update();
    }

    private bool IsEquippedDef(Item item)
    {
        return _pawn.Equipment.Potions.Any(p => p.Def == item.Def);
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

            RefreshEditors();
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

        RefreshEditors();
    }

    private void RefreshEditors()
    {
        _editors.Widgets.Clear();
        _editorList.Clear();
        foreach (var potion in _pawn.Equipment.Potions)
        {
            var editor = new PotionTriggerEditor(_gui, potion, () => UnequipPotion(potion));
            _editorList.Add(editor);
            _editors.Widgets.Add(editor);
        }

        if (_editorList.Count == 0)
        {
            _editors.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Click a potion below to equip it",
                TextColor = new Color(140, 140, 140)
            });
        }
    }
}

internal sealed class PotionTriggerEditor : VerticalStackPanel, IUpdatable
{
    private readonly Item _potion;
    private readonly OptionDropdown<PotionTriggerType> _typeDropdown;
    private readonly ThresholdSlider _slider;
    private readonly Label _summary;
    private readonly IReadOnlyList<PotionTriggerType> _allowed;

    public PotionTriggerEditor(BaseGui gui, Item potion, Action onUnequip)
    {
        _potion = potion;
        Spacing = 6;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        potion.PotionTrigger ??= potion.ItemDef.PotionProperties?.DefaultTrigger?.Clone()
                                 ?? new PotionTrigger { Type = PotionTriggerType.Immediately };

        _allowed = potion.ItemDef.PotionProperties?.GetAllowedTriggerTypes()
                   ?? Enum.GetValues<PotionTriggerType>();

        Widgets.Add(CreateHeader(gui, potion, onUnequip));

        var whenRow = new HorizontalStackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        _typeDropdown = new OptionDropdown<PotionTriggerType>(
            gui.Desktop,
            _allowed,
            TriggerLabels.For,
            potion.PotionTrigger.Type,
            ApplyType);
        whenRow.Widgets.Add(_typeDropdown);

        _slider = new ThresholdSlider(
            SliderCaption(potion.PotionTrigger.Type),
            SliderMode(potion.PotionTrigger.Type),
            CurrentSliderValue(potion.PotionTrigger));
        _slider.ValueChanged += ApplyValue;
        whenRow.Widgets.Add(_slider);
        Widgets.Add(whenRow);

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
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "✕" }
        };
        remove.Click += (_, _) => onUnequip();

        var row = new HorizontalStackPanel { Spacing = 8 };
        row.Widgets.Add(new Image
        {
            Background = potion.GetIconImage(),
            Width = BaseContent.IconSizes.Small,
            Height = BaseContent.IconSizes.Small,
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Widgets.Add(name);
        row.Widgets.Add(remove);
        return row;
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
                _potion.PotionTrigger.AfterSeconds = Math.Max(0, value);
                break;
            case PotionTriggerType.SelfBloodBelow:
            case PotionTriggerType.EnemyBloodBelow:
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
        }

        _summary.Text = TriggerLabels.Summarize(trigger);
    }

    private static ThresholdSliderMode SliderMode(PotionTriggerType type)
    {
        return type == PotionTriggerType.AfterSeconds
            ? ThresholdSliderMode.Seconds
            : ThresholdSliderMode.Percent;
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
