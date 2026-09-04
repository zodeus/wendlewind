using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class MedicinalPanel : EntityPanelBase
{
    private readonly Item _item;
    private Label? _stackValue;

    public MedicinalPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null)
        : base(gui, item, properties)
    {
        _item = item;
        var card = EntityCardChrome.BeginInspect(this, item);

        var stats = CreateStatStrip(item, item.ItemDef.MedicinalProperties, card.BodyWidth);
        if (stats != null)
        {
            Widgets.Add(stats);
        }

        var effect = ResolveEffect(item);
        if (!string.IsNullOrWhiteSpace(effect))
        {
            Widgets.Add(EntityCardChrome.SectionHeader("Effect"));
            Widgets.Add(EntityCardChrome.BodyLabel(effect, EntityCardChrome.Effect, card.BodyWidth));
        }

        var mechanics = ResolveMechanics(item).ToList();
        if (mechanics.Count > 0)
        {
            Widgets.Add(EntityCardChrome.SectionHeader("Core mechanics"));
            Widgets.Add(EntityCardChrome.MechanicsBlock(mechanics, card.BodyWidth));
        }
    }

    private Widget? CreateStatStrip(Item item, MedicinalProperties? medicinal, int bodyWidth)
    {
        var chips = new List<Widget>();

        if (MedicalChest.IsInfiniteUse(item.ItemDef))
        {
            chips.Add(EntityCardChrome.StatChip("Use", "Infinite", EntityCardChrome.Gold, out _));
        }
        else if (item.IsStackable)
        {
            chips.Add(EntityCardChrome.StatChip("Stack", $"x{item.StackSize}", EntityCardChrome.Gold, out _stackValue));
        }

        if (item.ItemDef.GoldCost > 0)
        {
            chips.Add(EntityCardChrome.StatChip("Cost", $"{item.ItemDef.GoldCost}g", EntityCardChrome.Gold, out _));
        }

        var cooldown = MedicalChest.CooldownInTicks(item.ItemDef);
        if (cooldown > 0)
        {
            chips.Add(EntityCardChrome.StatChip("Cooldown", FormatSeconds(cooldown), EntityCardChrome.Info, out _));
        }

        if (medicinal?.DurationInTicks > 0)
        {
            chips.Add(EntityCardChrome.StatChip("Duration", FormatSeconds(medicinal.DurationInTicks), EntityCardChrome.Effect, out _));
        }

        if (chips.Count == 0 && medicinal?.DefaultTrigger == null)
        {
            return null;
        }

        var column = new VerticalStackPanel { Spacing = 6 };
        if (chips.Count > 0)
        {
            column.Widgets.Add(EntityCardChrome.StatStrip(chips));
        }

        var trigger = medicinal?.DefaultTrigger;
        if (trigger != null)
        {
            column.Widgets.Add(EntityCardChrome.BodyLabel(
                "Chest · " + TriggerLabels.Summarize(trigger, null).Replace(" · auto target", ""),
                EntityCardChrome.Muted,
                bodyWidth));
        }

        return column;
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

    private static IEnumerable<string> ResolveMechanics(Item item)
    {
        var fromHandler = item.MedicinalHandler?.GetHowItWorks(item);
        if (fromHandler is { Count: > 0 })
        {
            foreach (var line in fromHandler)
            {
                yield return line;
            }

            yield break;
        }

        if (item.ItemDef.Moniker == "Cauterize")
        {
            yield return "Does not restore hit points.";
            yield return "Stops a severed stump from spraying.";
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
