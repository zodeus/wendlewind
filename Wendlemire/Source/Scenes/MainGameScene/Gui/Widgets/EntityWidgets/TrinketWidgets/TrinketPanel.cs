using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.TrinketWidgets;

public sealed class TrinketPanel : EntityPanelBase
{
    public TrinketPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        EntityCardChrome.BeginInspect(this, item);

        var chips = item.Def.BaseStats
            .Select(stat => (
                stat.Def.Label,
                item.GetStatValue(stat.Def).ToString(CultureInfo.InvariantCulture),
                Color.LightGoldenrodYellow))
            .ToList();

        var kills = item.TrinketHandler?.Kills ?? 0;
        if (kills > 0)
        {
            chips.Add(("Kills", kills.ToString(CultureInfo.InvariantCulture), EntityCardChrome.Gold));
        }

        if (chips.Count > 0)
        {
            Widgets.Add(EntityCardChrome.StatStrip(chips.ToArray()));
        }
    }

    public override void Update()
    {
    }
}
