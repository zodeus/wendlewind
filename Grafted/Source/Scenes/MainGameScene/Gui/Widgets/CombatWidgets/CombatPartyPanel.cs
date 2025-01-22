namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal sealed class CombatPartyPanel : VerticalStackPanel
{
    private readonly List<PawnCombatPanel> _panels;

    public CombatPartyPanel(CombatGui.ZoneGui gui, Encounter encounter, List<Pawn> pawns, HorizontalAlignment pawnAlignment)
    {
        Spacing = 0;
        ShowGridLines = false;
        _panels = new List<PawnCombatPanel>();
        HorizontalStackPanel pawnRow = new()
        {
            MinHeight = 380,
            //Border = new SolidBrush(Color.Aquamarine),
            //BorderThickness = new Thickness(1),
            Spacing = 15, HorizontalAlignment = pawnAlignment,
        };
        Widgets.Add(pawnRow);

        foreach (var pawn in pawns)
        {
            var isPlayer = pawn.PawnType == PawnType.Player;
            PawnCombatPanel panel = new(gui, pawn, encounter, isPlayer);
            _panels.Add(panel);
            pawnRow.Widgets.Add(panel);
        }
    }

    public void Update()
    {
        for (var i = _panels.Count - 1; i >= 0; i--)
        {
            var panel = _panels[i];
            panel.Update();
        }
    }
}