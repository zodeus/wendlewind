namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class ResourcePanel : EntityPanelBase
{
    private readonly Button _makePotionButton;

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

        var potion = EntityGenerator.CreateEntity<Item>(potionToMake);
        player.Pawn.Inventory.TryAdd(potion);
    }
    public override void Update()
    {
    }
}