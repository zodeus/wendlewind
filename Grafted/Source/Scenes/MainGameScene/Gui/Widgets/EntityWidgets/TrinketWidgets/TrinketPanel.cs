using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

public sealed class TrinketPanel : EntityPanelBase
{
    public TrinketPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;
        Widgets.Add(new Image { Background = new TextureRegion(item.Icon), Width = 128, Height = 128 });
        Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = item.Def.Description, Wrap = true, Width = 600 });
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