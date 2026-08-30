using System.Globalization;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

/// <summary>
/// Panel for displaying medicinal items. Uses the handler's GetInfoPanel() method
/// to display custom infographics if available, otherwise shows a default layout.
/// </summary>
public class MedicinalPanel : EntityPanelBase
{
    private readonly Item _item;
    private readonly Label? _stackLabel;
    private readonly Widget? _customContent;

    public MedicinalPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null) 
        : base(gui, item, properties)
    {
        _item = item;

        _customContent = null;

        if (_customContent != null)
        {
            // Use the custom panel content directly
            Widgets.Add(_customContent);
        }
        else
        {
            // Fall back to default consumable-style layout
            Padding = new Thickness(20);
            MinWidth = 300;
            
            _stackLabel = new Label("small")
            {
                Text = $"Stack Size: x{item.StackSize}",
                Margin = new Thickness(0, 0, 0, 15),
                Visible = item.IsStackable
            };
            
            Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 10,
                Widgets =
                {
                    new Image { Background = item.GetIconImage(), Width = 128, Height = 128 },
                    new Label(BaseContent.Styles.Label.Normal)
                    {
                        Text = item.Def.Description,
                        Wrap = true,
                        MaxWidth = 400,
                        Margin = new Thickness(0, 10, 0, 0)
                    },
                }
            });
            Widgets.Add(_stackLabel);

            var medicinal = item.ItemDef.MedicinalProperties;
            if (medicinal != null)
            {
                if (MedicalChest.IsInfiniteUse(item.ItemDef))
                {
                    Widgets.Add(new Label("small")
                    {
                        Text = "Infinite use",
                        TextColor = new Color(200, 180, 140),
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }

                var cooldown = MedicalChest.CooldownInTicks(item.ItemDef);
                if (cooldown > 0)
                {
                    Widgets.Add(new Label("small")
                    {
                        Text = $"Cooldown: {cooldown / (float)GameContext.TicksPerSecond:0.#}s",
                        TextColor = new Color(180, 180, 180),
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }

                if (medicinal.DurationInTicks > 0)
                {
                    Widgets.Add(new Label("small")
                    {
                        Text = $"Duration: {medicinal.DurationInTicks / (float)GameContext.TicksPerSecond:0.#}s",
                        TextColor = new Color(180, 180, 180),
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }

                var triggers = medicinal.GetAllowedTriggerTypes();
                if (triggers.Count > 0 && triggers.Count < Enum.GetValues<MedicalTriggerType>().Length)
                {
                    Widgets.Add(new Label("small")
                    {
                        Text = "Triggers: " + string.Join(", ", triggers.Select(TriggerLabels.For)),
                        TextColor = new Color(180, 180, 180),
                        Wrap = true,
                        MaxWidth = 400,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }

                Widgets.Add(new Label("small")
                {
                    Text = "Watches: " + string.Join(", ", medicinal.GetWatchPool().Select(TriggerLabels.For)),
                    TextColor = new Color(180, 180, 180),
                    Wrap = true,
                    MaxWidth = 400,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            foreach (var baseStat in item.Def.BaseStats)
            {
                var row = new HorizontalStackPanel { Spacing = 10 };
                row.Widgets.Add(new Label("small") { Text = $"{baseStat.Def.Label}:" });
                row.Widgets.Add(new Label("small") { Text = item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
                Widgets.Add(row);
            }
        }
    }

    public override void Update()
    {
        if (_stackLabel != null)
        {
            _stackLabel.Text = $"Stack Size: x{_item.StackSize}";
        }
    }
}
