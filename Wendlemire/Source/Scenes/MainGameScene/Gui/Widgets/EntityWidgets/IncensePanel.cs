namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class IncensePanel : EntityPanelBase
{
    private static readonly Color WarmGlow = new(255, 200, 120);

    private readonly Item _item;
    private Label? _stackValue;

    public IncensePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        var card = EntityCardChrome.BeginInspect(this, item, WarmGlow);

        var chips = new List<Widget>();
        var incenseProps = item.ItemDef.IncenseProperties;
        if (incenseProps?.Effect != null)
        {
            var durationSeconds = incenseProps.GetDurationInTicks() / (float)GameContext.TicksPerSecond;
            chips.Add(EntityCardChrome.StatChip("Duration", $"{durationSeconds:0.#}s", WarmGlow, out _));
        }

        if (item.StackSize > 1 || item.ItemDef.StackLimit > 1)
        {
            chips.Add(EntityCardChrome.StatChip("Stack", $"x{item.StackSize}", EntityCardChrome.Gold, out _stackValue));
        }

        if (chips.Count > 0)
        {
            Widgets.Add(EntityCardChrome.StatStrip(chips));
        }

        if (incenseProps?.Effect == null)
        {
            return;
        }

        var durationSecondsLit = incenseProps.GetDurationInTicks() / (float)GameContext.TicksPerSecond;
        Widgets.Add(EntityCardChrome.BodyLabel("Slots light at 120, 240, then 360", EntityCardChrome.Muted, card.BodyWidth));

        Widgets.Add(EntityCardChrome.SectionHeader("When Lit"));
        var rows = new List<Widget>
        {
            EntityCardChrome.IconLabel(
                new TextureRegion(incenseProps.Effect.Def.GetTexture()),
                incenseProps.Effect.Def.Label,
                IncenseProperties.GetEffectColor(incenseProps.Effect.Def),
                $"({durationSecondsLit:0.#}s)")
        };

        FoodPanel.AddAffectedStatRows(rows, incenseProps.Effect.Def.AffectedStats);
        Widgets.Add(EntityCardChrome.InsetBlock(card.BodyWidth, rows.ToArray()));
    }

    public override void Update()
    {
        if (_stackValue != null)
        {
            _stackValue.Text = $"x{_item.StackSize}";
        }
    }
}
