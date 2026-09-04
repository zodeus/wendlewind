namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class PotionPanel : EntityPanelBase
{
    private readonly Item _item;
    private Label? _stackValue;

    public PotionPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        _item = item;
        var card = EntityCardChrome.BeginInspect(this, item, GetPotionTitleColor(item));

        var chips = new List<Widget>();
        if (item.IsStackable)
        {
            chips.Add(EntityCardChrome.StatChip("Stack", $"x{item.StackSize}", EntityCardChrome.Gold, out _stackValue));
        }

        var potionDuration = (int)item.GetStatValue(Defs.Stats.PotionDuration);
        if (potionDuration > 0)
        {
            chips.Add(EntityCardChrome.StatChip("Duration", $"{potionDuration} ticks", EntityCardChrome.Info, out _));
        }

        var potionPower = item.GetStatValue(Defs.Stats.PotionPower);
        if (potionPower > 0 && potionPower != 1)
        {
            chips.Add(EntityCardChrome.StatChip("Power", $"{potionPower:0.##}x", EntityCardChrome.Effect, out _));
        }

        if (chips.Count > 0)
        {
            Widgets.Add(EntityCardChrome.StatStrip(chips));
        }

        var effectDescription = GetEffectDescription(item);
        if (!string.IsNullOrEmpty(effectDescription))
        {
            Widgets.Add(EntityCardChrome.SectionHeader("Effect"));
            Widgets.Add(EntityCardChrome.BodyLabel(effectDescription, EntityCardChrome.Effect, card.BodyWidth));
        }

        var triggerText = item.PotionTrigger?.Describe();
        if (!string.IsNullOrWhiteSpace(triggerText))
        {
            Widgets.Add(EntityCardChrome.BodyLabel(triggerText, EntityCardChrome.Muted, card.BodyWidth));
        }
    }

    private static bool IsHealingPotion(Item item) =>
        item.Def == Defs.Items.HealingPotion
        || item.Def == Defs.Items.HealingFlask
        || item.Def == Defs.Items.HealingSalve;

    private static Color GetPotionTitleColor(Item item)
    {
        if (item.Def == Defs.Items.JarOfBlood)
            return new Color(180, 40, 40);
        if (item.Def == Defs.Items.Pitchblood)
            return new Color(90, 20, 20);
        if (item.Def == Defs.Items.AcidFlask)
            return new Color(140, 200, 60);
        if (item.Def == Defs.Items.TallowFlask)
            return new Color(210, 180, 90);
        if (IsHealingPotion(item))
            return new Color(200, 80, 100);

        return BaseContent.Colors.Text.Golden;
    }

    private static string GetEffectDescription(Item item)
    {
        if (item.PotionHandler != null)
        {
            return item.PotionHandler.GetEffectDescription();
        }

        if (item.Def == Defs.Items.JarOfBlood)
            return "Instantly restores all lost blood.";
        if (item.Def == Defs.Items.AcidFlask)
            return "Throws acid at opponent, potentially blinding them.";
        if (IsHealingPotion(item))
            return "Applies regeneration to all body parts.";

        return "";
    }

    public override void Update()
    {
        if (_item.IsDestroyed)
        {
            return;
        }

        if (_stackValue != null)
        {
            _stackValue.Text = $"x{_item.StackSize}";
        }
    }
}
