using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

[UsedImplicitly]
public sealed class FoodPanel : EntityPanelBase
{
    private readonly Button _eatButton;
    private readonly Button _cookButton;

    public FoodPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
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
        /*Widgets.Add(new Label("small") { Text = $"Nutritional Value: {item.GetStatValue(Defs.Stats.NutritionalValue)}", Wrap = true });
        if (item.ItemDef.FoodProperties?.Effects.Any() == true)
        {
            Widgets.Add(new Label("small")
            {
                Text = $"Effects: {string.Join(", ", item.ItemDef.FoodProperties.Effects.Select(e => e.Def.Label))}", Wrap = true
            });
        }*/

        _eatButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Eat" }, Margin = new Thickness(0, 20, 0, 0)
        };
        _eatButton.Click += (_, _) =>
        {
            if (Core.Context.PlayerPawn.TryEat(item))
            {
                  gui.WorldTextHandler.Add(new WorldSpaceText
                {
                    Font = BaseContent.Fonts.Default.Medium,
                    Color = Color.PaleGoldenrod,
                    Text = item.Label,
                    DurationInTicks = 120,
                    Position = Mouse.GetState().Position.ToVector2()
                });
            }
            
        };

        _cookButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Cook" }, Margin = new Thickness(0, 20, 0, 0),
            Visible = ShowCookButton(item)
        };
        _cookButton.Click += (_, _) => { HandleCooking(item); };
        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 20,
            Widgets = { _eatButton, _cookButton }
        });
    }

    private static void HandleCooking(Item item)
    {
        if (item.ItemDef != Defs.Items.RawMeat && item.ItemDef != Defs.Items.RawCorn) return;
        if (item.StackSize > 1)
        {
            item.StackSize--;
        }
        else
        {
            item.Destroy();
        }

        if (item.ItemDef == Defs.Items.RawMeat)
        {
            Core.Context.PlayerPawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.CookedMeat));
        }
        else if (item.ItemDef == Defs.Items.RawCorn)
        {
            Core.Context.PlayerPawn.Inventory.TryAdd(EntityGenerator.CreateEntity<Item>(Defs.Items.CookedCorn));
        }
    }

    private static bool ShowCookButton(Item item)
    {
        if (item.ItemDef == Defs.Items.RawMeat && Core.Context.Player.HasTrinkets(Defs.Items.EncasedFire))
            return true;
        if (item.ItemDef == Defs.Items.RawCorn && Core.Context.Player.HasTrinkets(Defs.Items.EncasedFire, Defs.Items.CookingPot, Defs.Items.WeepingBucket))
            return true;
        return false;
    }

    public override void Update()
    {
        _eatButton.Enabled = Core.Context.World.PlayerPawn.IsHungry;
    }
}