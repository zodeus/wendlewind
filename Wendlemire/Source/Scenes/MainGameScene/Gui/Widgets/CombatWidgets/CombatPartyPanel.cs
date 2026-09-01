namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal sealed class CombatPartyPanel : VerticalStackPanel
{
    private readonly List<PawnCombatPanel> _panels;

    public CombatPartyPanel(
        BaseGui gui,
        Encounter encounter,
        List<Pawn> pawns,
        HorizontalAlignment pawnAlignment,
        bool includePortrait = true)
    {
        Spacing = 0;
        _panels = new List<PawnCombatPanel>();
        HorizontalStackPanel pawnRow = new()
        {
            Spacing = 15,
            HorizontalAlignment = pawnAlignment
        };
        Widgets.Add(pawnRow);

        foreach (var pawn in pawns)
        {
            PawnCombatPanel panel = new(gui, pawn, encounter, includePortrait);
            _panels.Add(panel);
            pawnRow.Widgets.Add(panel);
        }
    }

    public void Update(float deltaTime)
    {
        for (var i = _panels.Count - 1; i >= 0; i--)
        {
            var panel = _panels[i];
            panel.Update(deltaTime);
        }
    }

    /// <summary>
    /// Gets the combat panel for a specific pawn.
    /// </summary>
    public PawnCombatPanel? GetPanelForPawn(Pawn pawn)
    {
        return _panels.FirstOrDefault(p => p.Pawn == pawn);
    }

}