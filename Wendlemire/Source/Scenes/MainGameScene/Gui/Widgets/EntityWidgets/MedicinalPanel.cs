using Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnPreparationPanelWidgets;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class MedicinalPanel : EntityPanelBase
{
    private static readonly Color EffectGreen = new(140, 220, 140);
    private static readonly Color BodyGray = new(190, 190, 190);

    private readonly Item _item;
    private Label? _stackValue;

    public MedicinalPanel(BaseGui gui, Item item, EntityPanelProperties? properties = null)
        : base(gui, item, properties)
    {
        _item = item;
        Padding = new Thickness(20);
        MinWidth = 420;
        Spacing = 10;

        Widgets.Add(CreateHeader(item));
        Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 2, 0, 2) });
        Widgets.Add(CreateProperties(item, item.ItemDef.MedicinalProperties));

        var effect = ResolveEffect(item);
        if (!string.IsNullOrWhiteSpace(effect))
        {
            Widgets.Add(SectionHeader("Effect"));
            Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = effect,
                Wrap = true,
                MaxWidth = 380,
                TextColor = EffectGreen
            });
        }

        var how = ResolveHowItWorks(item).ToList();
        if (how.Count > 0)
        {
            Widgets.Add(SectionHeader("How it works"));
            foreach (var line in how)
            {
                Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = "• " + line,
                    Wrap = true,
                    MaxWidth = 380,
                    TextColor = BodyGray,
                    Margin = new Thickness(4, 0, 0, 2)
                });
            }
        }
    }

    private static HorizontalStackPanel CreateHeader(Item item)
    {
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

        var info = new VerticalStackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (!string.IsNullOrWhiteSpace(item.Def.Description) && item.Def.Description != "undefined")
        {
            info.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = item.Def.Description,
                Wrap = true,
                MaxWidth = 300,
                TextColor = Color.LightGray
            });
        }

        return new HorizontalStackPanel
        {
            Spacing = 16,
            Widgets = { iconFrame, info }
        };
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

        return props;
    }

    private static string ResolveEffect(Item item)
    {
        var fromHandler = item.MedicinalHandler?.GetEffectDescription(item);
        if (!string.IsNullOrWhiteSpace(fromHandler))
        {
            return fromHandler;
        }

        return item.ItemDef.Moniker == "Cauterize"
            ? "Seals an unsealed socket after a limb is severed."
            : string.Empty;
    }

    private static IEnumerable<string> ResolveHowItWorks(Item item)
    {
        var fromHandler = item.MedicinalHandler?.GetHowItWorks(item);
        if (fromHandler is { Count: > 0 })
        {
            foreach (var line in fromHandler)
            {
                yield return line;
            }
        }
        else if (item.ItemDef.Moniker == "Cauterize")
        {
            yield return "Does not restore hit points.";
            yield return "Stops a severed stump from spraying.";
        }

        var trigger = item.ItemDef.MedicinalProperties?.DefaultTrigger;
        if (trigger != null)
        {
            yield return "Chest: " + TriggerLabels.Summarize(trigger, null).Replace(" · auto target", "");
        }
    }

    private static Label SectionHeader(string text) =>
        new("small")
        {
            Text = text,
            TextColor = BaseContent.Colors.Text.Golden,
            Margin = new Thickness(0, 4, 0, 2)
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

    public override void Update()
    {
        if (_stackValue != null)
        {
            _stackValue.Text = $"x{_item.StackSize}";
        }
    }
}
