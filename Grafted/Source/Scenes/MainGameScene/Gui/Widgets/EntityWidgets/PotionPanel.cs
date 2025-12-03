using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public sealed class PotionPanel : EntityPanelBase
{
    public PotionPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
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
        var potionDuration = (int)item.GetStatValue(Defs.Stats.PotionDuration);
        if (potionDuration > 0)
        {
            Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Duration /c[{TC.Blue}]{potionDuration}/c[{TC.Default}] ticks", Wrap = true, Margin = new Thickness(10), Width = 600 });
        }

        if (item.ItemDef == Defs.Items.JarOfBlood)
        {
            TextButton button = new(BaseContent.Styles.Button.Normal) { Text = "Sip" };
            button.Click += (_, _) =>
            {
                Core.Context.PlayerPawn.Body.BloodAmount = Core.Context.PlayerPawn.Body.MaxBlood;
                Core.Context.Achievements.OnItemUsed(Core.Context.PlayerPawn, item);
                item.StackSize--;
                if (item.StackSize < 1)
                {
                    item.Destroy();
                }

                Core.Context.TickOnce();
            };
            Widgets.Add(button);
        }
    }

    public override void Update()
    {
    }
}