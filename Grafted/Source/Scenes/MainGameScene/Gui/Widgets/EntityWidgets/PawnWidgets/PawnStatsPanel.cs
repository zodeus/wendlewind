using System.Globalization;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public sealed class PawnStatsPanel : VerticalStackPanel, IUpdatable
{
    public PawnStatsPanel(Pawn pawn)
    {
        Spacing = 20;
        Padding = new Thickness(15);
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.Red];
        foreach (BaseStat baseStat in pawn.Def.BaseStats)
        {
            Widgets.Add(new HorizontalStackPanel
            {
                Widgets =
                {
                    new Label { Text = baseStat.Def.Label, Width = 250 },
                    new Label { Text = pawn.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) }
                }
            });
        }
    }

    public void Update()
    {
    }
}