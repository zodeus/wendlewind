using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class MedicinalPanel : EntityPanelBase
{
    private static readonly Color BodyGray = new(190, 190, 190);

    private readonly Item _item;
    private Label? _stackValue;

    public MedicinalPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null)
        : base(gui, item, properties)
    {
        _item = item;
        EntityCardChrome.ApplyCard(this, 340);

        Widgets.Add(EntityCardChrome.Header(item));
        Widgets.Add(CreateProperties(item, item.ItemDef.MedicinalProperties));

        var effect = ResolveEffect(item);
        if (!string.IsNullOrWhiteSpace(effect))
        {
            Widgets.Add(EntityCardChrome.SectionLabel("Effect"));
            Widgets.Add(EntityCardChrome.BodyLabel(effect, EntityCardChrome.Effect));
        }

        var how = ResolveHowItWorks(item).ToList();
        if (how.Count > 0)
        {
            Widgets.Add(EntityCardChrome.SectionLabel("How it works"));
            foreach (var line in how)
            {
                Widgets.Add(new Label("small")
                {
                    Text = "• " + line,
                    Wrap = true,
                    MaxWidth = 300,
                    TextColor = BodyGray
                });
            }
        }
    }

    private VerticalStackPanel CreateProperties(Item item, MedicinalProperties? medicinal)
    {
        var props = new VerticalStackPanel { Spacing = 1 };

        if (MedicalChest.IsInfiniteUse(item.ItemDef))
        {
            props.Widgets.Add(EntityCardChrome.StatRow("Use", "Infinite", ColorExt.HexToColor(TC.Golden.TrimStart('#'))));
        }
        else if (item.IsStackable)
        {
            _stackValue = new Label("small")
            {
                Text = $"x{item.StackSize}",
                TextColor = ColorExt.HexToColor(TC.Golden.TrimStart('#'))
            };
            props.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 6,
                Widgets =
                {
                    new Label("small") { Text = "Stack:", TextColor = EntityCardChrome.Muted },
                    _stackValue
                }
            });
        }

        if (item.ItemDef.GoldCost > 0)
        {
            props.Widgets.Add(EntityCardChrome.StatRow("Cost", $"{item.ItemDef.GoldCost}g", ColorExt.HexToColor(TC.Golden.TrimStart('#'))));
        }

        var cooldown = MedicalChest.CooldownInTicks(item.ItemDef);
        if (cooldown > 0)
        {
            props.Widgets.Add(EntityCardChrome.StatRow("Cooldown", FormatSeconds(cooldown), ColorExt.HexToColor(TC.Blue.TrimStart('#'))));
        }

        if (medicinal?.DurationInTicks > 0)
        {
            props.Widgets.Add(EntityCardChrome.StatRow("Duration", FormatSeconds(medicinal.DurationInTicks), ColorExt.HexToColor(TC.Green.TrimStart('#'))));
        }

        return props;
    }

    private static string ResolveEffect(Item item)
    {
        var fromHandler = item.MedicinalHandler?.GetEffectDescription(item);
        if (!string.IsNullOrWhiteSpace(fromHandler))
        {
            return fromHandler;
        }

        return item.ItemDef.Moniker == "Cauterize"
            ? "Seals an unsealed socket after a limb is severed."
            : string.Empty;
    }

    private static IEnumerable<string> ResolveHowItWorks(Item item)
    {
        var fromHandler = item.MedicinalHandler?.GetHowItWorks(item);
        if (fromHandler is { Count: > 0 })
        {
            foreach (var line in fromHandler)
            {
                yield return line;
            }
        }
        else if (item.ItemDef.Moniker == "Cauterize")
        {
            yield return "Does not restore hit points.";
            yield return "Stops a severed stump from spraying.";
        }

        var trigger = item.ItemDef.MedicinalProperties?.DefaultTrigger;
        if (trigger != null)
        {
            yield return "Chest: " + TriggerLabels.Summarize(trigger, null).Replace(" · auto target", "");
        }
    }

    private static string FormatSeconds(int ticks) =>
        $"{ticks / (float)GameContext.TicksPerSecond:0.#}s";

    public override void Update()
    {
        if (_stackValue != null)
        {
            _stackValue.Text = $"x{_item.StackSize}";
        }
    }
}
