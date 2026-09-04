namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class ResourcePanel : EntityPanelBase
{
    private readonly CursorButton _makePotionButton;

    public ResourcePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        var card = EntityCardChrome.BeginInspect(this, item);

        var ammoProps = item.ItemDef.AmmoProperties;
        if (ammoProps != null)
        {
            Widgets.Add(EntityCardChrome.StatStrip(
                ("Damage", $"{ammoProps.DamageRange.Min:F0}–{ammoProps.DamageRange.Max:F0}", Color.LightGray),
                ("Type", ammoProps.DamageType.ToString(), GetDamageTypeColor(ammoProps.DamageType))));

            if (ammoProps.BodyPartModifiers.Count > 0)
            {
                Widgets.Add(EntityCardChrome.SectionHeader("Effects"));
                foreach (var modifier in ammoProps.BodyPartModifiers)
                {
                    var chanceText = modifier.Chance.Min < 1f
                        ? $"{modifier.Chance.Min * 100:F0}%"
                        : "Always";
                    Widgets.Add(EntityCardChrome.InsetBlock(
                        card.BodyWidth,
                        new Label("small") { Text = modifier.Def.Label, TextColor = Color.IndianRed },
                        EntityCardChrome.StatRow("Chance", chanceText, EntityCardChrome.Muted)));
                }
            }
        }

        var makePotionTitle = TryGetMakePotionTitle(Core.Context.Player, item);
        _makePotionButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = $"Make {makePotionTitle}" },
            Margin = new Thickness(0, 6, 0, 0)
        };
        _makePotionButton.Click += (_, _) => MakePotion(Core.Context.Player, item);
        _makePotionButton.Visible = makePotionTitle != null;
        Widgets.Add(_makePotionButton);
    }

    private static Color GetDamageTypeColor(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Sharp => Color.LightSteelBlue,
            DamageType.Blunt => Color.SandyBrown,
            DamageType.Piercing => Color.Silver,
            DamageType.Flesh => Color.IndianRed,
            DamageType.Fire => Color.OrangeRed,
            DamageType.Ice => Color.LightCyan,
            DamageType.Acid => Color.LimeGreen,
            DamageType.Poison => Color.MediumSeaGreen,
            DamageType.Magic => Color.MediumPurple,
            _ => Color.Gray
        };
    }

    private static string? TryGetMakePotionTitle(Player player, Item item)
    {
        if (item.ItemDef == Defs.Items.HealingRoot
            && player.HasTrinkets(Defs.Items.MortarAndPestle)
            && player.HasTrinkets(Defs.Items.VialOfDuplicity)
            && player.HasTrinkets(Defs.Items.WeepingBucket))
        {
            return Defs.Items.BalmyOintment.Label;
        }

        return null;
    }

    private static void MakePotion(Player player, Item item)
    {
        var potionToMake = Defs.Items.BalmyOintment;

        if (item.StackSize > 1)
        {
            item.StackSize--;
        }
        else
        {
            item.Destroy();
        }

        var potion = Core.Context.Factory.CreateEntity<Item>(potionToMake);
        player.Pawn.Inventory.TryAdd(potion);
    }

    public override void Update()
    {
    }
}
