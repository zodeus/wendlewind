namespace Grafted.Scenes.MainGameScene.Gui.Widgets.DefWidgets;

internal class DefsPanel : Grid {
    public DefsPanel(IReadOnlyList<Def> defs, IBrush? background = null) {
        RowSpacing = 30;
        ColumnSpacing = 30;
        int gridRow = 0;
        int gridColum = 0;
        foreach (Def def in defs) {
            DefPanelBase panel = def.UiPanelFor(def, new DefPanelProperties { Background = background });
            panel.GridRow = gridRow;
            panel.GridColumn = gridColum;
            Widgets.Add(panel);

            gridColum++;
            if (gridColum > 2) {
                gridColum = 0;
                gridRow++;
            }
        }
    }
}