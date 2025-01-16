using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class PotionPanel : EntityPanelBase
{
    public PotionPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) : base(gui, item, properties)
    {
        Padding = new Thickness(20);
        MinWidth = 300;
        Spacing = 5;
        Widgets.Add(new Image { Background = new TextureRegion(item.Icon), Width = 128, Height = 128 });
        Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = item.Def.Description, Wrap = true, Margin = new Thickness(10), Width = 600 });
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