using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

public sealed class CombatFighterStatsColumn : Panel, IUpdatable
{
    private static readonly Color LabelColor = new(170, 165, 155);
    private static readonly Color ValueColor = new(200, 180, 120);
    private const int CellHeight = 40;

    private readonly Pawn _pawn;
    private readonly Label _bloodValue;
    private readonly HorizontalProgressBar _bloodBar;
    private readonly Label _hpValue;
    private readonly HorizontalProgressBar _hpBar;
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

        Place(grid, CreateMeterCell("Blood", out _bloodValue, out _bloodBar), 0, 0);
        Place(grid, CreateMeterCell("Body", out _hpValue, out _hpBar), 0, 1);

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
        _bloodValue.Text = $"{Mathf.RoundToInt(bloodPercent * 100)}%";
        _bloodValue.TextColor = BodyPartColor.GetBloodColor(bloodPercent);
        _bloodBar.Value = bloodPercent * 100;

        var attack = _pawn.AttackSpeed;
        _atk.Text = attack < 1 ? $"{attack:.00}" : $"{attack:0.0}";
        _atk.TextColor = attack < 1 ? Color.Red : attack < 2 ? Color.Orange : Color.YellowGreen;

        var energy = _pawn.Body.EnergyPercent;
        _enr.Text = $"{Mathf.RoundToInt(energy * 100)}%";
        _enr.TextColor = BodyPartColor.GetStomachColor(energy);

        var stomach = _pawn.Body.StomachLevel;
        _stm.Text = $"{Mathf.RoundToInt(stomach * 100)}%";
        _stm.TextColor = BodyPartColor.GetStomachColor(stomach);

        var period = _pawn.CalculateTicksToAttack();
        var remaining = Math.Clamp(_pawn.TicksToAttack, 0, period);
        _nxt.Text = remaining <= 0 ? "rdy" : $"{remaining / (float)GameContext.TicksPerSecond:0.0}s";
        _nxt.TextColor = ValueColor;

        var maxHp = _pawn.Body.MaxHitPoints;
        var hpPercent = maxHp <= 0 ? 0 : _pawn.Body.HitPoints / maxHp;
        _hpValue.Text = $"{Mathf.RoundToInt((float)hpPercent * 100)}%";
        _hpValue.TextColor = BodyPartColor.GetBloodColor((float)hpPercent);
        _hpBar.Value = (float)hpPercent * 100;

        _acc.Text = $"{_pawn.GetStatValue(Defs.Stats.Accuracy) * 100:0}%";
        _acc.TextColor = ValueColor;
        _eva.Text = $"{_pawn.GetStatValue(Defs.Stats.Evasion) * 100:0}%";
        _eva.TextColor = ValueColor;

        var temperature = _pawn.Body.Temperature;
        _tmp.Text = $"{temperature:0}";
        _tmp.TextColor = BodyPartColor.GetBodyTemperatureColor(temperature);

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

    private static Widget CreateMeterCell(string name, out Label value, out HorizontalProgressBar bar)
    {
        value = ValueLabel();
        bar = new HorizontalProgressBar(BaseContent.Styles.Bar.Health)
        {
            Height = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Value = 100
        };

        var header = new HorizontalStackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets = { NameLabel(name), value }
        };

        return Frame(new VerticalStackPanel
        {
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { header, bar }
        }, CellHeight + 16);
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
