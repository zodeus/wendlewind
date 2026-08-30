using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class CombatFighterStatsColumn : Panel, IUpdatable
{
    private static readonly Color LabelColor = new(170, 165, 155);
    private static readonly Color ValueColor = new(200, 180, 120);
    private const int CellHeight = 40;

    private readonly Pawn _pawn;
    private readonly CombatVitalMeter _bloodMeter;
    private readonly CombatVitalMeter _bodyMeter;
    private readonly Label _atk;
    private readonly Label _enr;
    private readonly Label _stm;
    private readonly Label _nxt;
    private readonly Label _acc;
    private readonly Label _eva;
    private readonly Label _tmp;
    private readonly PawnCapabilitiesOverlay _capabilities;
    private readonly Image _stanceIcon;
    private BodyStanceDef? _stance;

    public CombatFighterStatsColumn(Pawn pawn)
    {
        _pawn = pawn;
        Width = 180;
        Padding = new Thickness(0);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;

        var grid = new Grid
        {
            ColumnSpacing = 4,
            RowSpacing = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        for (var i = 0; i < 5; i++)
        {
            grid.RowsProportions.Add(Proportion.Auto);
        }

        _bloodMeter = new CombatVitalMeter(CombatVitalMeter.VitalKind.Blood, "Blood", CellHeight);
        _bodyMeter = new CombatVitalMeter(CombatVitalMeter.VitalKind.Body, "Body", CellHeight);
        Place(grid, _bloodMeter, 0, 0);
        Place(grid, _bodyMeter, 0, 1);

        Place(grid, CreateCell("Attack", out _atk), 1, 0);
        Place(grid, CreateCell("Energy", out _enr), 1, 1);
        Place(grid, CreateCell("Stomach", out _stm), 2, 0);
        Place(grid, CreateCell("Next", out _nxt), 2, 1);
        Place(grid, CreateCell("Accuracy", out _acc), 3, 0);
        Place(grid, CreateCell("Evasion", out _eva), 3, 1);
        Place(grid, CreateCell("Temp", out _tmp), 4, 0);
        var stanceCell = CreateIconCell("Stance", out _stanceIcon);
        stanceCell.WithTooltip(() => new Label(BaseContent.Styles.Label.Small)
        {
            Text = _pawn.Body.Stance?.Label ?? "Stance"
        });
        Place(grid, stanceCell, 4, 1);

        _capabilities = new PawnCapabilitiesOverlay(pawn.Body)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 0),
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Border = null,
            BorderThickness = new Thickness(0)
        };

        Widgets.Add(new VerticalStackPanel
        {
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Widgets = { grid, _capabilities }
        });
        Update();
    }

    public void Update()
    {
        var bloodPercent = _pawn.Body.BloodPercent;
        UiLabel.Set(_bloodMeter.ValueLabel, $"{Mathf.RoundToInt(bloodPercent * 100)}%", BodyPartColor.GetBloodColor(bloodPercent));
        _bloodMeter.Bar.Value = bloodPercent * 100;
        _bloodMeter.Fill = bloodPercent;
        _bloodMeter.Update();

        var attack = _pawn.AttackSpeed;
        UiLabel.Set(_atk, attack < 1 ? $"{attack:.00}" : $"{attack:0.0}",
            attack < 1 ? Color.Red : attack < 2 ? Color.Orange : Color.YellowGreen);

        var energy = _pawn.Body.EnergyPercent;
        UiLabel.Set(_enr, $"{Mathf.RoundToInt(energy * 100)}%", BodyPartColor.GetStomachColor(energy));

        var stomach = _pawn.Body.StomachLevel;
        UiLabel.Set(_stm, $"{Mathf.RoundToInt(stomach * 100)}%", BodyPartColor.GetStomachColor(stomach));

        var period = _pawn.CalculateTicksToAttack();
        var remaining = Math.Clamp(_pawn.TicksToAttack, 0, period);
        UiLabel.Set(_nxt, remaining <= 0 ? "rdy" : $"{remaining / (float)GameContext.TicksPerSecond:0.0}s", ValueColor);

        var maxHp = _pawn.Body.MaxHitPoints;
        var hpPercent = maxHp <= 0 ? 0 : _pawn.Body.HitPoints / maxHp;
        UiLabel.Set(_bodyMeter.ValueLabel, $"{Mathf.RoundToInt((float)hpPercent * 100)}%", BodyPartColor.GetBloodColor((float)hpPercent));
        _bodyMeter.Bar.Value = (float)hpPercent * 100;
        _bodyMeter.Fill = (float)hpPercent;
        _bodyMeter.Update();

        UiLabel.Set(_acc, $"{_pawn.GetStatValue(Defs.Stats.Accuracy) * 100:0}%", ValueColor);
        UiLabel.Set(_eva, $"{_pawn.GetStatValue(Defs.Stats.Evasion) * 100:0}%", ValueColor);

        var temperature = _pawn.Body.Temperature;
        UiLabel.Set(_tmp, $"{temperature:0}", BodyPartColor.GetBodyTemperatureColor(temperature));

        _capabilities.Update();

        var stance = _pawn.Body.Stance;
        if (stance != _stance)
        {
            _stance = stance;
            _stanceIcon.Background = stance != null
                ? new ColoredRegion(new TextureRegion(stance.GetTexture()), Color.White)
                : null;
        }
    }

    private static void Place(Grid grid, Widget cell, int row, int column)
    {
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        grid.Widgets.Add(cell);
    }

    private static Widget CreateIconCell(string name, out Image icon)
    {
        icon = new Image
        {
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        return Frame(new VerticalStackPanel
        {
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { NameLabel(name), icon }
        }, CellHeight);
    }

    private static Widget CreateCell(string name, out Label value)
    {
        value = ValueLabel();
        return Frame(new VerticalStackPanel
        {
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { NameLabel(name), value }
        }, CellHeight);
    }

    private static Panel Frame(Widget content, int height)
    {
        return new Panel
        {
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
            Padding = new Thickness(4, 2),
            Widgets = { content }
        };
    }

    private static Label NameLabel(string name)
    {
        return new Label(BaseContent.Styles.Label.Small)
        {
            Text = name,
            TextColor = LabelColor,
            HorizontalAlignment = HorizontalAlignment.Center
        };
    }

    private static Label ValueLabel()
    {
        return new Label(BaseContent.Styles.Label.Small)
        {
            TextColor = ValueColor,
            HorizontalAlignment = HorizontalAlignment.Center
        };
    }
}
