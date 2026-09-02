namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

public sealed class TrinketPanel : EntityPanelBase
{
    public TrinketPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        EntityCardChrome.ApplyCard(this);
        Widgets.Add(EntityCardChrome.Header(item));
        var kills = item.TrinketHandler?.Kills ?? 0;
        if (kills > 0)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Kills /c[{TC.Golden}]{kills}" });
        }
    }

    public override void Update()
    {
    }
}