using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public enum PawnVitalsLayout
{
    Horizontal,
    Vertical
}

public sealed class PawnVitalsPanel : Panel, IUpdatable
{
    private static readonly Color StatDivider = new(40, 38, 35);

    private readonly Pawn _pawn;
    private readonly AttackSpeedIcon _attackSpeed;
    private readonly Label _bloodLabel;
    private readonly Label _energyLabel;
    private readonly Image _bloodArrow;
    private readonly Image _stomachGauge;
    private readonly Panel _stomachContainer;
    private readonly HorizontalProgressBar _bloodBar;
    private readonly HorizontalProgressBar _energyBar;

    public PawnVitalsPanel(Pawn pawn, PawnVitalsLayout layout = PawnVitalsLayout.Horizontal)
    {
        _pawn = pawn;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        var compact = layout == PawnVitalsLayout.Vertical;
        var font = compact ? BaseContent.Fonts.Default.Small : BaseContent.Fonts.Default.Medium;
        var iconSize = compact ? 24 : 36;
        var groupPadding = compact ? new Thickness(4, 3) : new Thickness(16, 8);
        var barWidth = compact ? 56 : 80;
        var barHeight = compact ? 6 : 8;

        _bloodArrow = new Image
        {
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowNegative], Color.DarkGray),
            Width = compact ? 16 : 24,
            Height = compact ? 16 : 24,
            VerticalAlignment = VerticalAlignment.Center
        };

        _bloodLabel = new Label
        {
            Font = font,
            Width = compact ? 40 : 56,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _bloodBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Width = barWidth,
            Height = barHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Value = 100
        };

        _attackSpeed = new AttackSpeedIcon(pawn, font)
        {
            Height = compact ? 28 : 44,
            Width = compact ? 52 : 76,
            VerticalAlignment = VerticalAlignment.Center
        };

        _energyLabel = new Label
        {
            Font = font,
            Width = compact ? 40 : 56,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _energyBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Energy)
        {
            Width = barWidth,
            Height = barHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Value = 100
        };

        _stomachGauge = new Image
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = compact ? 24 : 32,
            Height = compact ? 24 : 32,
            Background = new ColoredRegion(new TextureRegion(Defs.BodyParts.Stomach.GetIcon()), Color.White)
        };

        _stomachContainer = new Panel
        {
            Width = compact ? 30 : 40,
            Height = compact ? 30 : 40,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.StomachOutline], Color.White),
            Widgets = { _stomachGauge }
        };

        var bloodGroup = CreateStatGroup(
            Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Blood],
            new Color(180, 40, 40),
            _bloodArrow,
            _bloodLabel,
            _bloodBar,
            iconSize,
            groupPadding);

        var attackGroup = new Panel
        {
            Padding = groupPadding,
            VerticalAlignment = VerticalAlignment.Stretch,
            Widgets =
            {
                new HorizontalStackPanel
                {
                    Spacing = compact ? 4 : 8,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Widgets =
                    {
                        new Image
                        {
                            Width = iconSize,
                            Height = iconSize,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.AttackSpeed]
                        },
                        _attackSpeed
                    }
                }
            }
        };

        var energyGroup = CreateStatGroup(
            Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Energy],
            new Color(220, 180, 40),
            null,
            _energyLabel,
            _energyBar,
            iconSize,
            groupPadding);

        var hungerGroup = new Panel
        {
            Padding = groupPadding,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { _stomachContainer }
        };

        if (layout == PawnVitalsLayout.Horizontal)
        {
            Width = 600;
            var row = new HorizontalStackPanel
            {
                Spacing = 0,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            row.Widgets.Add(bloodGroup);
            row.Widgets.Add(CreateVerticalDivider());
            row.Widgets.Add(attackGroup);
            row.Widgets.Add(CreateVerticalDivider());
            row.Widgets.Add(energyGroup);
            row.Widgets.Add(CreateVerticalDivider());
            row.Widgets.Add(hungerGroup);
            Widgets.Add(row);
        }
        else
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            var column = new VerticalStackPanel
            {
                Spacing = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            column.Widgets.Add(bloodGroup);
            column.Widgets.Add(CreateHorizontalDivider());
            column.Widgets.Add(attackGroup);
            column.Widgets.Add(CreateHorizontalDivider());
            column.Widgets.Add(energyGroup);
            column.Widgets.Add(CreateHorizontalDivider());
            column.Widgets.Add(hungerGroup);
            Widgets.Add(column);
        }

        Update();
    }

    public void Update()
    {
        _attackSpeed.Update();

        if (_pawn.Body.BloodChangeLastFrame < 0)
        {
            _bloodArrow.Background = new ColoredRegion(
                Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowNegative], Color.White);
        }
        else
        {
            _bloodArrow.Background = new ColoredRegion(
                Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.ArrowNegative], Color.Transparent);
        }

        var bloodPercent = _pawn.Body.BloodPercent;
        UiLabel.Set(_bloodLabel, $"{Mathf.RoundToInt(bloodPercent * 100)}%", BodyPartColor.GetBloodColor(bloodPercent));
        _bloodBar.Value = bloodPercent * 100;

        var energyPercent = _pawn.Body.EnergyPercent;
        UiLabel.Set(_energyLabel, $"{Mathf.RoundToInt(energyPercent * 100)}%", BodyPartColor.GetStomachColor(energyPercent));
        _energyBar.Value = energyPercent * 100;

        var stomachLevel = _pawn.Body.StomachLevel;
        _stomachGauge.Background = new ColoredRegion(
            Stylesheet.Current.Atlas["stomach-" + Mathf.RoundToInt(Mathf.Lerp(1, 16, stomachLevel))],
            BodyPartColor.GetStomachColor(stomachLevel));
        ((ColoredRegion)_stomachContainer.Background).Color = BodyPartColor.GetStomachColor(stomachLevel);
    }

    private static Panel CreateStatGroup(
        IImage icon,
        Color iconTint,
        Image? arrow,
        Label valueLabel,
        HorizontalProgressBar bar,
        int iconSize,
        Thickness padding)
    {
        var iconImage = new Image
        {
            Width = iconSize,
            Height = iconSize,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new ColoredRegion((TextureRegion)icon, iconTint)
        };

        var valueStack = new VerticalStackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { valueLabel, bar }
        };

        var content = new HorizontalStackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (arrow != null)
        {
            content.Widgets.Add(arrow);
        }

        content.Widgets.Add(iconImage);
        content.Widgets.Add(valueStack);

        return new Panel
        {
            Padding = padding,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { content }
        };
    }

    private static Panel CreateVerticalDivider()
    {
        return new Panel
        {
            Width = 1,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 6),
            Background = new SolidBrush(StatDivider)
        };
    }

    private static Panel CreateHorizontalDivider()
    {
        return new Panel
        {
            Height = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(8, 0),
            Background = new SolidBrush(StatDivider)
        };
    }
}
