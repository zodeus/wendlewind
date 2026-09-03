using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

public sealed class TrinketPanel : EntityPanelBase
{
    public TrinketPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        EntityCardChrome.ApplyCard(this);
        Widgets.Add(EntityCardChrome.Header(item));
        foreach (var baseStat in item.Def.BaseStats)
        {
            Widgets.Add(EntityCardChrome.StatRow(
                baseStat.Def.Label,
                item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture)));
        }

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