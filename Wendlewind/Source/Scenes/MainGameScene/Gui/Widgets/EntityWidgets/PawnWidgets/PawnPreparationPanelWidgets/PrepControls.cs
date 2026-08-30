namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

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
    Seconds
}

internal sealed class ThresholdSlider : HorizontalStackPanel
{
    private readonly Label _caption;
    private readonly HorizontalSlider _slider;
    private readonly Label _valueLabel;
    private ThresholdSliderMode _mode = ThresholdSliderMode.Percent;

    public event Action<float>? ValueChanged;

    public ThresholdSlider(string caption, ThresholdSliderMode mode, float value)
    {
        Spacing = 8;
        VerticalAlignment = VerticalAlignment.Center;

        _caption = new Label(BaseContent.Styles.Label.Small)
        {
            Text = caption,
            TextColor = new Color(160, 160, 160),
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 56
        };
        Widgets.Add(_caption);

        _slider = new HorizontalSlider
        {
            Width = 110,
            VerticalAlignment = VerticalAlignment.Center
        };
        _slider.ValueChangedByUser += (_, _) =>
        {
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

    public float StoredValue => _mode == ThresholdSliderMode.Percent
        ? Math.Clamp(_slider.Value / 100f, 0f, 1f)
        : Math.Max(0f, _slider.Value);

    public void Configure(ThresholdSliderMode mode, float value)
    {
        _mode = mode;
        if (mode == ThresholdSliderMode.Percent)
        {
            _slider.Minimum = 0;
            _slider.Maximum = 100;
            _slider.Value = Math.Clamp(value * 100f, 0f, 100f);
        }
        else
        {
            _slider.Minimum = 0;
            _slider.Maximum = 60;
            _slider.Value = Math.Clamp(value, 0f, 60f);
        }

        RefreshValueLabel();
    }

    public void SetCaption(string caption)
    {
        _caption.Text = caption;
    }

    private void RefreshValueLabel()
    {
        _valueLabel.Text = _mode == ThresholdSliderMode.Percent
            ? TriggerLabels.Percent(StoredValue)
            : TriggerLabels.Seconds(StoredValue);
    }
}

internal sealed class OptionDropdown<T> : CursorButton, IUpdatable
{
    private const int PopupCloseDistance = 10;

    private readonly Desktop _desktop;
    private readonly Func<T, string> _labelSelector;
    private readonly Action<T> _onSelect;
    private readonly Label _label;
    private IReadOnlyList<T> _items;
    private Window? _popup;

    public OptionDropdown(
        Desktop desktop,
        IReadOnlyList<T> items,
        Func<T, string> labelSelector,
        T current,
        Action<T> onSelect) : base(BaseContent.Styles.Button.Small)
    {
        _desktop = desktop;
        _items = items;
        _labelSelector = labelSelector;
        _onSelect = onSelect;

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

        MinWidth = 128;
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
            _popup.Top,
            contentBounds.Width + styleBuffer,
            contentBounds.Height + styleBuffer
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

        var list = new VerticalStackPanel { Spacing = 0 };
        foreach (var item in _items)
        {
            var captured = item;
            var option = new CursorButton(BaseContent.Styles.Button.Dark)
            {
                Content = new Label(BaseContent.Styles.Label.Small)
                {
                    Text = _labelSelector(captured)
                },
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            option.Click += (_, _) =>
            {
                _label.Text = _labelSelector(captured);
                _onSelect(captured);
                Close();
            };
            list.Widgets.Add(option);
        }

        _popup.Content = list;
        var uiPos = Core.ScreenToUi(Mouse.GetState().Position);
        _popup.Show(_desktop, uiPos);
    }

    private void Close()
    {
        _popup?.Close();
        _popup = null;
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
