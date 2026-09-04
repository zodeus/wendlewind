using System.Globalization;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class ConsumablePanel : EntityPanelBase
{
    private readonly Item _item;
    private Label? _stackValue;

    public ConsumablePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        EntityCardChrome.BeginInspect(this, item);

        var chips = new List<Widget>();
        if (item.IsStackable)
        {
            chips.Add(EntityCardChrome.StatChip("Stack", $"x{item.StackSize}", EntityCardChrome.Gold, out _stackValue));
        }

        foreach (var baseStat in item.Def.BaseStats)
        {
            chips.Add(EntityCardChrome.StatChip(
                baseStat.Def.Label,
                item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture),
                Color.LightGoldenrodYellow,
                out _));
        }

        if (chips.Count > 0)
        {
            Widgets.Add(EntityCardChrome.StatStrip(chips));
        }
    }

    public override void Update()
    {
        if (_stackValue != null)
        {
            _stackValue.Text = $"x{_item.StackSize}";
        }
    }
}
