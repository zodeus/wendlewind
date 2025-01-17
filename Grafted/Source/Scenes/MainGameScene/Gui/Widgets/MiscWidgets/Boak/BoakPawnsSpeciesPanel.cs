namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;

internal sealed class BoakPawnsSpeciesPanel : VerticalStackPanel
{
    public BoakPawnsSpeciesPanel(IReadOnlyList<PawnDef> defs)
    {
        Spacing = 20;
        var races = DefRepository<RaceDef>.Defs;
        foreach (var pawnDef in defs)
        {
            var pawnPanel = new VerticalStackPanel();
            Widgets.Add(pawnPanel);

            pawnPanel.Widgets.Add(new Label { Text = pawnDef.Label });
            pawnPanel.Widgets.Add(new Label { Text = pawnDef.Body.Label });
            pawnPanel.Widgets.Add(new Label { Text = pawnDef.Body.BloodType?.ToString() });
            pawnPanel.Widgets.Add(new Label { Text = string.Join(", ", races.Where(r => r.Species == pawnDef).Select(s => s.Label)) });
        }
    }
}