namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal class CombatPartyPanel : VerticalStackPanel
{
    private readonly List<PawnCombatPanel> _panels;

    public CombatPartyPanel(CombatGui.ZoneGui gui, Encounter encounter, List<Pawn> pawns, HorizontalAlignment pawnAlignment)
    {
        Spacing = 5;
        ShowGridLines = false;
        _panels = new List<PawnCombatPanel>();
        HorizontalStackPanel pawnRow = new()
        {
            MinHeight = 380,
            //Border = new SolidBrush(Color.Aquamarine),
            //BorderThickness = new Thickness(1),
            Spacing = 15, HorizontalAlignment = pawnAlignment,
            Margin = new Thickness(30, 0, 30, 0),
        };
        AddChild(pawnRow);

        foreach (Pawn pawn in pawns)
        {
            bool isPlayer = pawn.PawnType == PawnType.Player;
            PawnCombatPanel panel = new(gui, pawn, encounter, isPlayer);
            _panels.Add(panel);
            pawnRow.AddChild(panel);
        }
    }

    public void Update()
    {
        for (int i = _panels.Count - 1; i >= 0; i--)
        {
            PawnCombatPanel panel = _panels[i];
            if (panel.Pawn.IsDead)
            {
                panel.RemoveFromParent();
                _panels.RemoveAt(i);
                //_deathRow.AddChild(new Image { Background = new TextureRegion(panel.Pawn.Icon), Width = 56, Height = 56, VerticalAlignment = VerticalAlignment.Center });
                continue;
            }

            panel.Update();
        }
    }
}