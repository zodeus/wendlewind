namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class TrinketsPanel : PrepCard
{
    public TrinketsPanel(BaseGui gui, Pawn pawn) : base("Trinkets")
    {
        Body.Widgets.Add(Section("Combat", new TrinketBar(pawn.Inventory, TrinketType.Combat, item => gui.ViewEntity(item))
        {
            TrinketsPerRow = 6
        }));
        Body.Widgets.Add(Section("Passive", new TrinketBar(pawn.Inventory, TrinketType.Passive, item => gui.ViewEntity(item))
        {
            TrinketsPerRow = 6
        }));
        Body.Widgets.Add(Section("Interactive", new TrinketBar(pawn.Inventory, TrinketType.Interactive, item => gui.ViewEntity(item))
        {
            TrinketsPerRow = 6
        }));
    }

    private static Widget Section(string title, Widget content)
    {
        return new VerticalStackPanel
        {
            Spacing = 4,
            Widgets =
            {
                new Label(BaseContent.Styles.Label.Small)
                {
                    Text = title,
                    TextColor = new Color(160, 160, 160)
                },
                content
            }
        };
    }
}
