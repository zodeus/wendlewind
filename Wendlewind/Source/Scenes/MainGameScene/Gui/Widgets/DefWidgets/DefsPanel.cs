namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.DefWidgets;

internal class DefsPanel : Grid {
    public DefsPanel(IReadOnlyList<Def> defs, IBrush? background = null) {
        RowSpacing = 30;
        ColumnSpacing = 30;
        int gridRow = 0;
        int gridColum = 0;
        foreach (Def def in defs) {
            DefPanelBase panel = DefPanelFactory.Create(def, new DefPanelProperties { Background = background });
            Grid.SetRow(panel, gridRow);
            Grid.SetColumn(panel, gridColum);
            Widgets.Add(panel);

            gridColum++;
            if (gridColum > 2) {
                gridColum = 0;
                gridRow++;
            }
        }
    }
}