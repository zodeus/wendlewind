using FontStashSharp.RichText;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

internal static class TriggerLabels
{
    public static string For(PotionTriggerType type)
    {
        return type switch
        {
            PotionTriggerType.Immediately => "At battle start",
            PotionTriggerType.AfterSeconds => "After a delay",
            PotionTriggerType.SelfBloodBelow => "My blood drops below",
            PotionTriggerType.EnemyBloodBelow => "Enemy blood below",
            PotionTriggerType.SelfPartsDamaged => "My parts damaged past",
            _ => type.ToString()
        };
    }

    public static string For(MedicalTriggerType type)
    {
        return type switch
        {
            MedicalTriggerType.Immediately => "At battle start",
            MedicalTriggerType.AfterSeconds => "After a delay",
            MedicalTriggerType.SelfBloodBelow => "My blood drops below",
            MedicalTriggerType.SelfPartsDamaged => "My parts damaged past",
            MedicalTriggerType.PartBelowHealth => "A part drops below",
            MedicalTriggerType.PartSevered => "A part is severed",
            MedicalTriggerType.HasNecrosis => "A part has necrosis",
            MedicalTriggerType.BurningOrAcid => "A part is burning or acid",
            _ => type.ToString()
        };
    }

    public static string For(MedicalTargetSelector selector)
    {
        return selector switch
        {
            MedicalTargetSelector.Auto => "Auto",
            MedicalTargetSelector.MostDamagedPart => "Most damaged part",
            MedicalTargetSelector.SeveredOrUnsealedSocket => "Open socket",
            MedicalTargetSelector.SpecificPart => "Chosen part",
            _ => selector.ToString()
        };
    }

    public static string For(MedicalTargetPool pool)
    {
        return pool switch
        {
            MedicalTargetPool.External => "external parts",
            MedicalTargetPool.Internal => "organs",
            MedicalTargetPool.Artery => "arteries",
            MedicalTargetPool.Bone => "bones",
            MedicalTargetPool.Socket => "open sockets",
            MedicalTargetPool.Self => "whole body",
            _ => pool.ToString()
        };
    }

    public static string Percent(float value) => $"{value * 100f:0}%";

    public static string Seconds(float value) => $"{value:0.##}s";

    public static string Summarize(PotionTrigger trigger)
    {
        return trigger.Type switch
        {
            PotionTriggerType.Immediately => "Drinks at the start of battle",
            PotionTriggerType.AfterSeconds => $"Drinks after {Seconds(trigger.AfterSeconds)}",
            PotionTriggerType.SelfBloodBelow => $"Drinks when my blood drops below {Percent(trigger.Threshold)}",
            PotionTriggerType.EnemyBloodBelow => $"Drinks when enemy blood drops below {Percent(trigger.Threshold)}",
            PotionTriggerType.SelfPartsDamaged => $"Drinks when {Percent(trigger.Threshold)} of parts are damaged",
            _ => trigger.Type.ToString()
        };
    }

    public static string Summarize(MedicalTrigger trigger, string? partLabel)
    {
        var when = trigger.Type switch
        {
            MedicalTriggerType.Immediately => "Uses at the start of battle",
            MedicalTriggerType.AfterSeconds => $"Uses after {Seconds(trigger.AfterSeconds)}",
            MedicalTriggerType.SelfBloodBelow => $"Uses when my blood drops below {Percent(trigger.Threshold)}",
            MedicalTriggerType.SelfPartsDamaged => $"Uses when {Percent(trigger.Threshold)} of parts are damaged",
            MedicalTriggerType.PartBelowHealth => $"Uses when a part drops below {Percent(trigger.HealthThreshold)}",
            MedicalTriggerType.PartSevered => "Uses when a part is severed",
            MedicalTriggerType.HasNecrosis => "Uses when a part has necrosis",
            MedicalTriggerType.BurningOrAcid => "Uses when a part is burning or acid-burned",
            _ => trigger.Type.ToString()
        };

        var target = trigger.TargetSelector switch
        {
            MedicalTargetSelector.Auto => "auto target",
            MedicalTargetSelector.MostDamagedPart => "most damaged part",
            MedicalTargetSelector.SeveredOrUnsealedSocket => "open socket",
            MedicalTargetSelector.SpecificPart => partLabel ?? "chosen part",
            _ => trigger.TargetSelector.ToString()
        };

        return $"{when} · {target}";
    }
}

internal enum ThresholdSliderMode
{
    Percent,
    Blood,
    Seconds
}

internal sealed class ThresholdSlider : HorizontalStackPanel
{
    public const float MinSeconds = 2f;
    public const float MaxSeconds = 60f;
    public const float MinBlood = 0.05f;
    public const float MaxBlood = 1f;

    private const float MinBloodPercent = MinBlood * 100f;
    private const float MaxBloodPercent = MaxBlood * 100f;
    private const float BloodPercentStep = 5f;

    private readonly Label _caption;
    private readonly HorizontalSlider _slider;
    private readonly Label _valueLabel;
    private ThresholdSliderMode _mode = ThresholdSliderMode.Percent;

    public event Action<float>? ValueChanged;

    public ThresholdSlider(string caption, ThresholdSliderMode mode, float value)
    {
        Spacing = 6;
        VerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _caption = new Label(BaseContent.Styles.Label.Small)
        {
            Text = caption,
            TextColor = new Color(160, 160, 160),
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 36
        };
        Widgets.Add(_caption);

        _slider = new HorizontalSlider
        {
            MinWidth = 48,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        SetProportionType(_slider, ProportionType.Fill);
        _slider.ValueChangedByUser += (_, _) =>
        {
            SnapValue();
            RefreshValueLabel();
            ValueChanged?.Invoke(StoredValue);
        };
        Widgets.Add(_slider);

        _valueLabel = new Label(BaseContent.Styles.Label.Small)
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextColor = Color.White,
            MinWidth = 40
        };
        Widgets.Add(_valueLabel);

        Configure(mode, value);
    }

    public float StoredValue => _mode switch
    {
        ThresholdSliderMode.Blood => Math.Clamp(_slider.Value / 100f, MinBlood, MaxBlood),
        ThresholdSliderMode.Seconds => Math.Clamp(_slider.Value, MinSeconds, MaxSeconds),
        _ => Math.Clamp(_slider.Value / 100f, 0f, 1f)
    };

    public void Configure(ThresholdSliderMode mode, float value)
    {
        _mode = mode;
        switch (mode)
        {
            case ThresholdSliderMode.Blood:
                _slider.Minimum = MinBloodPercent;
                _slider.Maximum = MaxBloodPercent;
                _slider.Value = SnapToStep(value * 100f, MinBloodPercent, MaxBloodPercent, BloodPercentStep);
                break;
            case ThresholdSliderMode.Seconds:
                _slider.Minimum = MinSeconds;
                _slider.Maximum = MaxSeconds;
                _slider.Value = SnapToStep(value, MinSeconds, MaxSeconds, 1f);
                break;
            default:
                _slider.Minimum = 0;
                _slider.Maximum = 100;
                _slider.Value = Math.Clamp(value * 100f, 0f, 100f);
                break;
        }

        RefreshValueLabel();
    }

    public void SetCaption(string caption)
    {
        _caption.Text = caption;
    }

    private void RefreshValueLabel()
    {
        _valueLabel.Text = _mode == ThresholdSliderMode.Seconds
            ? TriggerLabels.Seconds(StoredValue)
            : TriggerLabels.Percent(StoredValue);
    }

    private void SnapValue()
    {
        var snapped = _mode switch
        {
            ThresholdSliderMode.Blood => SnapToStep(_slider.Value, MinBloodPercent, MaxBloodPercent, BloodPercentStep),
            ThresholdSliderMode.Seconds => SnapToStep(_slider.Value, MinSeconds, MaxSeconds, 1f),
            _ => _slider.Value
        };

        if (Math.Abs(_slider.Value - snapped) > 0.001f)
        {
            _slider.Value = snapped;
        }
    }

    private static float SnapToStep(float value, float min, float max, float step)
    {
        return Math.Clamp(MathF.Round(value / step) * step, min, max);
    }
}

internal sealed class OptionDropdown<T> : CursorButton, IUpdatable
{
    private const int PopupCloseDistance = 10;

    private readonly Desktop _desktop;
    private readonly Func<T, string> _labelSelector;
    private readonly Action<T> _onSelect;
    private readonly int _maxItemsPerColumn;
    private readonly Func<T, string>? _groupSelector;
    private readonly Label _label;
    private IReadOnlyList<T> _items;
    private Window? _popup;

    public OptionDropdown(
        Desktop desktop,
        IReadOnlyList<T> items,
        Func<T, string> labelSelector,
        T current,
        Action<T> onSelect,
        int maxItemsPerColumn = 0,
        Func<T, string>? groupSelector = null) : base(BaseContent.Styles.Button.Small)
    {
        _desktop = desktop;
        _items = items;
        _labelSelector = labelSelector;
        _onSelect = onSelect;
        _maxItemsPerColumn = maxItemsPerColumn;
        _groupSelector = groupSelector;

        _label = new Label(BaseContent.Styles.Label.Small)
        {
            Text = labelSelector(current),
            VerticalAlignment = VerticalAlignment.Center
        };

        Content = new HorizontalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets =
            {
                _label,
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "▾",
                    TextColor = new Color(180, 180, 180),
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        MinWidth = 96;
        Click += (_, _) => Show();
    }

    public void SetItems(IReadOnlyList<T> items, T current)
    {
        _items = items;
        SetCurrent(current);
    }

    public void SetCurrent(T current)
    {
        _label.Text = _labelSelector(current);
    }

    public void Update()
    {
        if (_popup?.IsPlaced != true)
        {
            return;
        }

        var contentPos = Core.ScreenToUi(Mouse.GetState().Position);
        var boundsOffset = new Point(
            (int)(Core.UiOffset.X / Core.UiScale),
            (int)(Core.UiOffset.Y / Core.UiScale)
        );
        var uiMousePos = new Point(contentPos.X + boundsOffset.X, contentPos.Y + boundsOffset.Y);

        var contentBounds = _popup.Content?.Bounds ?? _popup.Bounds;
        const int styleBuffer = 20;
        var popupBounds = new Rectangle(
            _popup.Left,
            _popup.Top - Bounds.Height,
            Math.Max(contentBounds.Width + styleBuffer, Bounds.Width),
            contentBounds.Height + styleBuffer + Bounds.Height
        );

        var expandedBounds = new Rectangle(
            popupBounds.X - PopupCloseDistance,
            popupBounds.Y - PopupCloseDistance,
            popupBounds.Width + PopupCloseDistance * 2,
            popupBounds.Height + PopupCloseDistance * 2
        );

        if (!expandedBounds.Contains(uiMousePos.X, uiMousePos.Y))
        {
            Close();
        }
    }

    private void Show()
    {
        if (_popup?.IsPlaced == true || _items.Count == 0)
        {
            return;
        }

        _popup = new Window
        {
            Title = null,
            Background = null,
            Padding = new Thickness(0)
        };
        _popup.TitlePanel.Visible = false;
        _popup.Content = BuildOptions();
        _popup.HorizontalAlignment = HorizontalAlignment.Left;
        _popup.VerticalAlignment = VerticalAlignment.Top;
        _popup.Show(_desktop, _desktop.ToLocal(ToGlobal(new Point(0, Bounds.Height))));
    }

    private Widget BuildOptions()
    {
        if (_groupSelector != null)
        {
            return BuildGroupedOptions();
        }

        var perColumn = _maxItemsPerColumn > 0 ? _maxItemsPerColumn : _items.Count;
        if (_items.Count <= perColumn)
        {
            var list = new VerticalStackPanel { Spacing = 0, MinWidth = Bounds.Width };
            foreach (var item in _items)
            {
                list.Widgets.Add(CreateOption(item));
            }

            return list;
        }

        var columns = new HorizontalStackPanel { Spacing = 0 };
        VerticalStackPanel? column = null;
        for (var i = 0; i < _items.Count; i++)
        {
            if (i % perColumn == 0)
            {
                column = new VerticalStackPanel { Spacing = 0 };
                columns.Widgets.Add(column);
            }

            column!.Widgets.Add(CreateOption(_items[i]));
        }

        return columns;
    }

    private Widget BuildGroupedOptions()
    {
        var groups = new List<(string Title, List<T> Items)>();
        var index = new Dictionary<string, List<T>>();
        foreach (var item in _items)
        {
            var title = _groupSelector!(item);
            if (!index.TryGetValue(title, out var items))
            {
                items = [];
                index[title] = items;
                groups.Add((title, items));
            }

            items.Add(item);
        }

        var columns = new HorizontalStackPanel { Spacing = 0 };
        foreach (var (title, items) in groups)
        {
            var column = new VerticalStackPanel { Spacing = 0 };
            var header = new Panel
            {
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(10, 6)
            };
            header.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = title,
                TextColor = new Color(200, 180, 140)
            });
            column.Widgets.Add(header);
            foreach (var item in items)
            {
                column.Widgets.Add(CreateOption(item));
            }

            columns.Widgets.Add(column);
        }

        return columns;
    }

    private CursorButton CreateOption(T item)
    {
        var option = new CursorButton(BaseContent.Styles.Button.Dark)
        {
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = _labelSelector(item)
            },
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        option.Click += (_, _) =>
        {
            _label.Text = _labelSelector(item);
            _onSelect(item);
            Close();
        };
        return option;
    }

    private void Close()
    {
        _popup?.Close();
        _popup = null;
    }
}

internal sealed class ChargeStepper : HorizontalStackPanel
{
    private readonly Label _value;
    private readonly CursorButton _minus;
    private readonly CursorButton _plus;
    private readonly CursorButton _max;
    private readonly Label _infinite;

    public event Action? Decrement;
    public event Action? Increment;
    public event Action? LoadMax;

    public ChargeStepper(bool compact = false)
    {
        Spacing = compact ? 4 : 6;
        VerticalAlignment = VerticalAlignment.Center;

        if (!compact)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = "Loaded",
                TextColor = new Color(160, 160, 160),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        _minus = StepButton("−");
        _minus.Click += (_, _) => Decrement?.Invoke();
        Widgets.Add(_minus);

        _value = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "0",
            MinWidth = 24,
            TextAlign = TextHorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Widgets.Add(_value);

        _plus = StepButton("+");
        _plus.Click += (_, _) => Increment?.Invoke();
        Widgets.Add(_plus);

        _max = new CursorButton(BaseContent.Styles.Button.Small)
        {
            Content = new Label(BaseContent.Styles.Label.Small) { Text = "Max" }
        };
        _max.Click += (_, _) => LoadMax?.Invoke();
        Widgets.Add(_max);

        _infinite = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "∞",
            TextColor = new Color(200, 180, 140),
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false
        };
        Widgets.Add(_infinite);
    }

    public void Set(int charges, bool canDecrement, bool canIncrement)
    {
        _infinite.Visible = false;
        _minus.Visible = true;
        _plus.Visible = true;
        _max.Visible = true;
        _value.Visible = true;
        _value.Text = charges.ToString();
        _minus.Enabled = canDecrement;
        _plus.Enabled = canIncrement;
        _max.Enabled = canIncrement;
    }

    public void SetInfinite()
    {
        _value.Visible = false;
        _minus.Visible = false;
        _plus.Visible = false;
        _max.Visible = false;
        _infinite.Visible = true;
    }

    private static CursorButton StepButton(string text)
    {
        return new CursorButton(BaseContent.Styles.Button.Small)
        {
            MinWidth = 22,
            Content = new Label(BaseContent.Styles.Label.Small)
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
    }
}

internal sealed class FillGauge : Panel
{
    private readonly Panel _fill;
    private readonly Label _label;

    public FillGauge(int width = 220, int height = 18)
    {
        Width = width;
        Height = height;
        Background = new SolidBrush(new Color(25, 25, 30));

        _fill = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidBrush(new Color(180, 130, 50))
        };
        Widgets.Add(_fill);

        _label = new Label(BaseContent.Styles.Label.Small)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextColor = Color.White
        };
        Widgets.Add(_label);
    }

    public void Set(float current, float max, string text)
    {
        var ratio = max <= 0 ? 0 : Math.Clamp(current / max, 0f, 1f);
        _fill.Width = (int)Math.Round((Width ?? 220) * ratio);
        _label.Text = text;
    }
}
