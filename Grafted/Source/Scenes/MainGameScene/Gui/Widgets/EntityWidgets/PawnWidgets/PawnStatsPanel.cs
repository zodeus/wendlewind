using System.Globalization;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public sealed class PawnStatsPanel : VerticalStackPanel, IUpdatable
{
    private readonly Pawn _pawn;
    private Dictionary<BaseStat, Label> _stats = new();

    public PawnStatsPanel(Pawn pawn)
    {
        _pawn = pawn;
        Spacing = 20;
        Padding = new Thickness(15);
        foreach (var baseStat in pawn.Def.BaseStats)
        {
            var label = new Label { Text = pawn.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture)};
            _stats.Add(baseStat, label);
            Widgets.Add(new HorizontalStackPanel
            {
                Widgets =
                {
                    new Label { Text = baseStat.Def.Label, Width = 300},
                    label
                }
            });
        }
    }

    public void Update()
    {
        foreach (var (stat, label) in _stats)
        {
            label.Text = _pawn.GetStatValue(stat.Def).ToString(CultureInfo.InvariantCulture);
        }
    }
}