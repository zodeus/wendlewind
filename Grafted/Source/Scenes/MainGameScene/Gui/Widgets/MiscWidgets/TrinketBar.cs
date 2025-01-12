using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

public sealed class TrinketBar : HorizontalStackPanel
{
    public TrinketBar(BaseGui gui, EntityContainer container)
    {
        void CreatePanel(Item trinket)
        {
            if (trinket.ItemDef.ItemType != ItemType.Trinket)
            {
                return;
            }

            var panel = new Button()
            {
                Width = 74, Height = 74,
                Padding = new Thickness(12),
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
                Content = new Image()
                {
                    VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = new TextureRegion(trinket.Icon)
                }
            };
            panel.Click += (_, _) => gui.ViewEntity(trinket);
            Widgets.Add(panel);
        }

        container.ItemAdded += CreatePanel;
        foreach (var trinket in container.AsItems().Where(i => i.ItemDef.ItemType == ItemType.Trinket))
        {
            CreatePanel(trinket);
        }
    }
}