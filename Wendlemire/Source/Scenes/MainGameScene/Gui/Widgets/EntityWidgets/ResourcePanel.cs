namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class ResourcePanel : EntityPanelBase
{
    private readonly CursorButton _makePotionButton;

    public ResourcePanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;

        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 10,
            Widgets =
            {
                new Image { Background = item.GetIconImage(), Width = 128, Height = 128 },
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = item.Def.Description, Wrap = true, MaxWidth = 400,
                    Margin = new Thickness(0, 10, 0, 0)
                },
            }
        });

        // AMMO PROPERTIES
        var ammoProps = item.ItemDef.AmmoProperties;
        if (ammoProps != null)
        {
            var ammoPanel = new VerticalStackPanel
            {
                Spacing = 5,
                Margin = new Thickness(0, 10, 0, 0)
            };

            ammoPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = "Ammo Properties",
                TextColor = Color.Gold
            });

            ammoPanel.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 10,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = "Damage Type:",
                        TextColor = Color.Gray
                    },
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = ammoProps.DamageType.ToString(),
                        TextColor = GetDamageTypeColor(ammoProps.DamageType)
                    }
                }
            });

            ammoPanel.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 10,
                Widgets =
                {
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = "Damage:",
                        TextColor = Color.Gray
                    },
                    new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = $"{ammoProps.DamageRange.Min:F0}-{ammoProps.DamageRange.Max:F0}",
                        TextColor = Color.LightGray
                    }
                }
            });

            if (ammoProps.BodyPartModifiers.Count > 0)
            {
                ammoPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "Effects:",
                    TextColor = Color.Gray,
                    Margin = new Thickness(0, 5, 0, 0)
                });

                foreach (var modifier in ammoProps.BodyPartModifiers)
                {
                    var chanceText = modifier.Chance.Min < 1f
                        ? $" ({modifier.Chance.Min * 100:F0}%)"
                        : "";
                    ammoPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                    {
                        Text = $"  • {modifier.Def.Label}{chanceText}",
                        TextColor = Color.IndianRed,
                        Margin = new Thickness(10, 0, 0, 0)
                    });
                }
            }

            Widgets.Add(ammoPanel);
        }

        // MAKE POTION
        var makePotionTitle = TryGetMakePotionTitle(Core.Context.Player, item);
        _makePotionButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = $"Make {makePotionTitle}" },
            Margin = new Thickness(0, 20, 0, 0)
        };
        _makePotionButton.Click += (_, _) => MakePotion(Core.Context.Player, item);
        _makePotionButton.Visible = makePotionTitle != null;

        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 20,
            Widgets = { _makePotionButton }
        });
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

    private string? TryGetMakePotionTitle(Player player, Item item)
    {
        if (item.ItemDef == Defs.Items.HealingRoot)
        {
            if (player.HasTrinkets(Defs.Items.MortarAndPestle) && player.HasTrinkets(Defs.Items.VialOfDuplicity) && player.HasTrinkets(Defs.Items.WeepingBucket))
            {
                return Defs.Items.BalmyOintment.Label;
            }
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