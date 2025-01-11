using Grafted.Sim.Entities;

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
        Widgets.Add(new Image { Background = new TextureRegion(item.Icon), Width = 64, Height = 64 });
        Widgets.Add(new Label("small") { Text = item.Def.Description, Wrap = true, Margin = new Thickness(0, 10, 0, 10), MaxWidth = 600 });

        _makePotionButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Make Potion" }, Margin = new Thickness(0, 20, 0, 0)
        };
        _makePotionButton.Click += (_, _) => MakePotion(Core.Context.Player, item);
        _makePotionButton.Visible = ShowMakePotionButton(Core.Context.Player, item);

        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 20,
            Widgets = { _makePotionButton }
        });
    }

    private bool ShowMakePotionButton(Player player, Item item)
    {
        if (item.ItemDef == Defs.Items.HealingRoot)
        {
            return player.HasTrinkets(Defs.Items.MortarAndPestle) && player.HasTrinkets(Defs.Items.VialOfDuplicity);
        }
        return false;
    }

    private static void MakePotion(Player player, Item item)
    {
        if (item.ItemDef == Defs.Items.HealingRoot)
        {
            if (item.StackSize > 1)
            {
                item.StackSize--;
            }
            else
            {
                item.Destroy();
            }

            player.Pawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.BalmyOintment));
        }
    }

    private static bool ShowCookButton(Item item)
    {
        if (item.ItemDef == Defs.Items.RawMeat && Core.Context.Player.HasTrinkets(Defs.Items.EncasedFire))
            return true;
        else if (item.ItemDef == Defs.Items.RawCorn && Core.Context.Player.HasTrinkets(Defs.Items.EncasedFire, Defs.Items.CookingPot, Defs.Items.EndlessWaterBucket))
            return true;
        return false;
    }

    public override void Update()
    {
        _makePotionButton.Enabled = Core.Context.World.PlayerPawn.IsHungry;
    }
}