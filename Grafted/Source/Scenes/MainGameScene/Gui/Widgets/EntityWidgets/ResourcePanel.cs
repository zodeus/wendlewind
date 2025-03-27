using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class ResourcePanel : EntityPanelBase
{
    private readonly Button _makePotionButton;
    private readonly Button _burnWoodButton;

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
                new Image { Background = new TextureRegion(item.Icon), Width = 128, Height = 128 },
                new Label(BaseContent.Styles.Label.Normal)
                {
                    Text = item.Def.Description, Wrap = true, MaxWidth = 400,
                    Margin = new Thickness(0, 10, 0, 0)
                },
            }
        });

        // MAKE POTION
        var makePotionTitle = TryGetMakePotionTitle(Core.Context.Player, item);
        _makePotionButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = $"Make {makePotionTitle}" }, Margin = new Thickness(0, 20, 0, 0)
        };
        _makePotionButton.Click += (_, _) => MakePotion(Core.Context.Player, item);
        _makePotionButton.Visible = makePotionTitle != null;

        // BURN Wood
        _burnWoodButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Burn Wood" }, Margin = new Thickness(0, 20, 0, 0)
        };
        _burnWoodButton.Click += (_, _) => BurnWood(gui, Core.Context.Player, item);
        _burnWoodButton.Visible = ShowBurnWood(Core.Context.Player, item);

        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 20,
            Widgets = { _makePotionButton, _burnWoodButton }
        });
    }

    private void BurnWood(BaseGui gui, Player player, Item item)
    {
        if (item.StackSize > 1)
        {
            item.StackSize--;
        }
        else
        {
            item.Destroy();
        }

        if (item.ItemDef == Defs.Items.GlitteringLog)
        {
            gui.PushScreenMessage(new ScreenMessageData
            {
                Font = BaseContent.Fonts.Default.Medium,
                Text = Defs.BodyEffects.SmokeyHaze.Description,
                Duration = 6,
                Color = Color.Orange
            });
            player.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = Defs.BodyEffects.SmokeyHaze,
                TicksLeft = 4000
            });
        }
        else if (item.ItemDef == Defs.Items.ShimmeringBark)
        {
            gui.PushScreenMessage(new ScreenMessageData
            {
                Font = BaseContent.Fonts.Default.Medium,
                Text = Defs.BodyEffects.SmokeyHaze.Description,
                Duration = 6,
                Color = Color.Orange
            });
            player.Pawn.Body.Effects.TryApplyEffect(new BodyEffect
            {
                Def = Defs.BodyEffects.Psychedelic,
                TicksLeft = 4000
            });
        }
    }

    private bool ShowBurnWood(Player player, Item item)
    {
        if (item.ItemDef == Defs.Items.GlitteringLog || item.ItemDef == Defs.Items.ShimmeringBark)
        {
            return player.HasTrinkets(Defs.Items.EncasedFire);
        }

        return false;
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

        player.Pawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(potionToMake));
    }

    public override void Update()
    {
    }
}