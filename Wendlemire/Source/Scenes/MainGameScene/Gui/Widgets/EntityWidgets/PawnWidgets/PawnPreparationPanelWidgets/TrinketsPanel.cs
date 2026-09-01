using Wendlemire.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

public sealed class TrinketsPanel : PrepCard, IUpdatable
{
    private readonly TrinketBar _combat;
    private readonly TrinketBar _passive;
    private readonly TrinketBar _interactive;

    public TrinketsPanel(BaseGui gui, Pawn pawn) : base("Trinkets")
    {
        _combat = new TrinketBar(pawn.Inventory, TrinketType.Combat, item => gui.ViewEntity(item));
        _passive = new TrinketBar(pawn.Inventory, TrinketType.Passive, item => gui.ViewEntity(item));
        _interactive = new TrinketBar(pawn.Inventory, TrinketType.Interactive, item => gui.ViewEntity(item));
        Body.Widgets.Add(Section("Combat", _combat));
        Body.Widgets.Add(Section("Passive", _passive));
        Body.Widgets.Add(Section("Interactive", _interactive));
    }

    public void Update()
    {
        _combat.Update();
        _passive.Update();
        _interactive.Update();
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
