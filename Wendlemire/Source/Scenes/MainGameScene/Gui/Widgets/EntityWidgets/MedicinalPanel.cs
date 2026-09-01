using System.Globalization;
using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class MedicinalPanel : EntityPanelBase
{
    private readonly Item _item;
    private Label? _stackValue;

    public MedicinalPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null)
        : base(gui, item, properties)
    {
        _item = item;
        Padding = new Thickness(20);
        MinWidth = 480;
        Spacing = 4;

        var mainLayout = new HorizontalStackPanel { Spacing = 20 };
        mainLayout.Widgets.Add(CreateLeftColumn(item));
        mainLayout.Widgets.Add(new VerticalSeparator());

        var right = new VerticalStackPanel { Spacing = 8 };
        var medicinal = item.ItemDef.MedicinalProperties;
        right.Widgets.Add(CreateProperties(item, medicinal));

        if (medicinal != null)
        {
            AddNamedList(right, "Triggers", medicinal.GetAllowedTriggerTypes().Select(TriggerLabels.For));
            AddNamedList(right, "Watches", medicinal.GetWatchPool().Select(TriggerLabels.For));
            AddNamedList(right, "Targets", medicinal.GetAllowedTargetSelectors().Select(TriggerLabels.For));
        }

        if (item.Def.BaseStats.Count > 0)
        {
            right.Widgets.Add(CreateStats(item));
        }

        mainLayout.Widgets.Add(right);
        Widgets.Add(mainLayout);
    }

    private static VerticalStackPanel CreateLeftColumn(Item item)
    {
        var left = new VerticalStackPanel { Spacing = 10, MinWidth = 220 };

        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4),
            Width = 80,
            Height = 80
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = item.GetIconImage(),
            Width = 72,
            Height = 72,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        left.Widgets.Add(iconFrame);

        if (!string.IsNullOrWhiteSpace(item.Def.Description) && item.Def.Description != "undefined")
        {
            left.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = item.Def.Description,
                Wrap = true,
                MaxWidth = 220,
                TextColor = Color.LightGray
            });
        }

        return left;
    }

    private VerticalStackPanel CreateProperties(Item item, MedicinalProperties? medicinal)
    {
        var props = new VerticalStackPanel { Spacing = 2 };

        if (MedicalChest.IsInfiniteUse(item.ItemDef))
        {
            props.Widgets.Add(CreatePropertyRow("Use", "Infinite", TC.Golden));
        }
        else if (item.IsStackable)
        {
            _stackValue = new Label("small")
            {
                Text = $"x{item.StackSize}",
                TextColor = ColorExt.HexToColor(TC.Golden.TrimStart('#'))
            };
            props.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 8,
                Widgets =
                {
                    new Label("small") { Text = "Stack:", TextColor = Color.Gray },
                    _stackValue
                }
            });
        }

        if (item.ItemDef.GoldCost > 0)
        {
            props.Widgets.Add(CreatePropertyRow("Cost", $"{item.ItemDef.GoldCost}g", TC.Golden));
        }

        var cooldown = MedicalChest.CooldownInTicks(item.ItemDef);
        if (cooldown > 0)
        {
            props.Widgets.Add(CreatePropertyRow("Cooldown", FormatSeconds(cooldown), TC.Blue));
        }

        if (medicinal?.DurationInTicks > 0)
        {
            props.Widgets.Add(CreatePropertyRow("Duration", FormatSeconds(medicinal.DurationInTicks), TC.Green));
        }

        if (medicinal != null)
        {
            props.Widgets.Add(CreatePropertyRow("Apply", ApplyLabel(medicinal.ApplyMode), TC.Purple));
            if (medicinal.DefaultTrigger != null)
            {
                props.Widgets.Add(CreatePropertyRow("Default", TriggerLabels.For(medicinal.DefaultTrigger.Type), TC.Golden));
            }
        }

        return props;
    }

    private static Widget CreateStats(Item item)
    {
        var section = new VerticalStackPanel { Spacing = 2 };
        section.Widgets.Add(SectionHeader("Stats"));

        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 1, Margin = new Thickness(4, 0, 0, 0) };
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var row = 0;
        foreach (var baseStat in item.Def.BaseStats)
        {
            var key = new Label("small") { Text = $"{baseStat.Def.Label}:", TextColor = Color.Gray };
            Grid.SetRow(key, row);
            Grid.SetColumn(key, 0);
            grid.Widgets.Add(key);

            var value = new Label("small")
            {
                Text = item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture),
                TextColor = Color.LightGoldenrodYellow
            };
            Grid.SetRow(value, row);
            Grid.SetColumn(value, 1);
            grid.Widgets.Add(value);
            row++;
        }

        section.Widgets.Add(grid);
        return section;
    }

    private static void AddNamedList(VerticalStackPanel parent, string title, IEnumerable<string> values)
    {
        var items = values.ToList();
        if (items.Count == 0)
        {
            return;
        }

        var section = new VerticalStackPanel { Spacing = 2 };
        section.Widgets.Add(SectionHeader(title));
        foreach (var value in items)
        {
            section.Widgets.Add(new Label("small")
            {
                Text = value,
                TextColor = Color.LightGray,
                Margin = new Thickness(4, 0, 0, 0)
            });
        }

        parent.Widgets.Add(section);
    }

    private static Label SectionHeader(string text) =>
        new("small")
        {
            Text = text,
            TextColor = BaseContent.Colors.Text.Golden,
            Margin = new Thickness(0, 0, 0, 2)
        };

    private static HorizontalStackPanel CreatePropertyRow(string key, string value, string valueColorHex)
    {
        var hex = valueColorHex.StartsWith('#') ? valueColorHex[1..] : valueColorHex;
        return new HorizontalStackPanel
        {
            Spacing = 8,
            Widgets =
            {
                new Label("small") { Text = $"{key}:", TextColor = Color.Gray },
                new Label("small") { Text = value, TextColor = ColorExt.HexToColor(hex) }
            }
        };
    }

    private static string FormatSeconds(int ticks) =>
        $"{ticks / (float)GameContext.TicksPerSecond:0.#}s";

    private static string ApplyLabel(MedicalApplyMode mode) => mode switch
    {
        MedicalApplyMode.Self => "Self",
        MedicalApplyMode.NearestExternalAncestor => "Nearest skin",
        _ => "Watched part"
    };

    public override void Update()
    {
        if (_stackValue != null)
        {
            _stackValue.Text = $"x{_item.StackSize}";
        }
    }
}
