using System.Globalization;

namespace Grafted.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class MendersMixHandler : MedicinalHandler
{
    private double _healAmount;

    // Colors for the infographic
    private static readonly Color FleshColor = new(180, 120, 120);       // Pinkish for flesh
    private static readonly Color BoneColor = new(200, 200, 210);        // Light grayish-blue for bone
    private static readonly Color SkinColor = new(210, 180, 150);        // Tan for skin
    private static readonly Color OrganColor = new(140, 80, 80);         // Dark red for organs
    private static readonly Color SocketColor = new(160, 140, 100);      // Gold-ish for socket connections
    private static readonly Color BalmColor = new(180, 200, 140);        // Soft green for soothing
    private static readonly Color HealedTextColor = new(130, 200, 130);
    private static readonly Color FlowArrowColor = new(100, 180, 100);
    private const double SoothingBalmPower = 1;

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var healingValue = item.GetStatValue(Defs.Stats.HealingValue);
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        
        _healAmount = healingValue;
        ApplyToPart(part, duration);
        
        return _healAmount < healingValue;
    }

    public override Widget? GetInfoPanel(Item item)
    {
        var panel = new VerticalStackPanel
        {
            Padding = new Thickness(20),
            MinWidth = 340,
            Spacing = 8
        };

        // Header Section: Icon + Description
        var headerSection = new HorizontalStackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(4),
            Width = 72,
            Height = 72
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = new TextureRegion(item.Icon),
            Width = 64,
            Height = 64,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        headerSection.Widgets.Add(iconFrame);

        var descPanel = new VerticalStackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        descPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = item.Def.Description,
            Wrap = true,
            MaxWidth = 240
        });
        headerSection.Widgets.Add(descPanel);
        panel.Widgets.Add(headerSection);

        // Stack size
        if (item.IsStackable)
        {
            panel.Widgets.Add(new Label("small")
            {
                Text = $"Stack Size: x{item.StackSize}",
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        // Stats (Healing Amount)
        foreach (var baseStat in item.Def.BaseStats)
        {
            var row = new HorizontalStackPanel { Spacing = 8 };
            row.Widgets.Add(new Label("small") { Text = $"{baseStat.Def.Label}:" });
            row.Widgets.Add(new Label("small")
            {
                Text = item.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture),
                TextColor = HealedTextColor
            });
            panel.Widgets.Add(row);
        }

        // Duration info
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        panel.Widgets.Add(new Label("small")
        {
            Text = $"Soothing Balm Duration: {duration} ticks",
            TextColor = BalmColor
        });

        // How It Works section
        panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 12, 0, 8) });

        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "How It Works",
            TextColor = BaseContent.Colors.Text.Golden,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Widgets.Add(CreateInfographic());
        panel.Widgets.Add(CreateLegend(item));

        return panel;
    }

    private static Widget CreateInfographic()
    {
        var container = new Panel
        {
            Background = new SolidBrush(new Color(25, 25, 30)),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var content = new VerticalStackPanel { Spacing = 6 };

        // Step 1: Apply
        content.Widgets.Add(CreateStepRow("1.", "Apply to body part", FleshColor));

        // Visual: What it heals
        var partsContainer = new Panel
        {
            Background = new SolidBrush(new Color(35, 35, 40)),
            Padding = new Thickness(10),
            Margin = new Thickness(20, 4, 20, 4)
        };

        var partsContent = new VerticalStackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        partsContent.Widgets.Add(new Label("small")
        {
            Text = "Heals ALL Part Types:",
            TextColor = HealedTextColor,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var partsRow = new HorizontalStackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        partsRow.Widgets.Add(CreatePartBox("Flesh", FleshColor));
        partsRow.Widgets.Add(CreatePartBox("Bone", BoneColor));
        partsRow.Widgets.Add(CreatePartBox("Skin", SkinColor));
        partsRow.Widgets.Add(CreatePartBox("Organ", OrganColor));
        partsContent.Widgets.Add(partsRow);

        partsContainer.Widgets.Add(partsContent);
        content.Widgets.Add(partsContainer);

        // Step 2: Spreads
        content.Widgets.Add(CreateStepRow("2.", "Spreads through sockets", SocketColor));

        // Visual: Flow arrows
        var flowContainer = new Panel
        {
            Background = new SolidBrush(new Color(35, 35, 40)),
            Padding = new Thickness(8, 6),
            Margin = new Thickness(20, 0, 20, 4)
        };

        var flowRow = new HorizontalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        flowRow.Widgets.Add(CreateFlowNode("Part", FleshColor));
        flowRow.Widgets.Add(CreateArrow());
        flowRow.Widgets.Add(CreateFlowNode("Socket", SocketColor));
        flowRow.Widgets.Add(CreateArrow());
        flowRow.Widgets.Add(CreateFlowNode("Part", FleshColor));
        flowRow.Widgets.Add(CreateArrow());
        flowRow.Widgets.Add(new Label("small") { Text = "...", TextColor = FlowArrowColor, VerticalAlignment = VerticalAlignment.Center });

        flowContainer.Widgets.Add(flowRow);
        content.Widgets.Add(flowContainer);

        // Step 3: Applies balm
        content.Widgets.Add(CreateStepRow("3.", "Applies Soothing Balm to all", BalmColor));

        container.Widgets.Add(content);
        return container;
    }

    private static HorizontalStackPanel CreateStepRow(string stepNum, string text, Color textColor)
    {
        return new HorizontalStackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 4, 0, 4),
            Widgets =
            {
                new Label("small")
                {
                    Text = stepNum,
                    TextColor = BaseContent.Colors.Text.Golden,
                    Width = 20
                },
                new Label("small")
                {
                    Text = text,
                    TextColor = textColor
                }
            }
        };
    }

    private static Widget CreatePartBox(string label, Color color)
    {
        return new Panel
        {
            Background = new SolidBrush(color),
            Padding = new Thickness(4, 2),
            Widgets =
            {
                new Label("small")
                {
                    Text = label,
                    TextColor = Color.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Scale = new Vector2(0.75f, 0.75f)
                }
            }
        };
    }

    private static Widget CreateFlowNode(string label, Color bgColor)
    {
        return new Panel
        {
            Background = new SolidBrush(bgColor),
            Padding = new Thickness(4, 2),
            Widgets =
            {
                new Label("small")
                {
                    Text = label,
                    TextColor = Color.White,
                    Scale = new Vector2(0.7f, 0.7f)
                }
            }
        };
    }

    private static Widget CreateArrow()
    {
        return new Label("small")
        {
            Text = "→",
            TextColor = FlowArrowColor,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Widget CreateLegend(Item item)
    {
        var legendPanel = new Panel
        {
            Background = new SolidBrush(new Color(30, 30, 35)),
            Padding = new Thickness(12)
        };

        var content = new VerticalStackPanel { Spacing = 6 };
        var grid = new Grid
        {
            ColumnSpacing = 16,
            RowSpacing = 4
        };
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        AddLegendRow(grid, 0, "✓ Heals:", "All part types (flesh, bone, skin, organs)", HealedTextColor);
        AddLegendRow(grid, 1, "⟳ Spreads:", "Through sockets to external parts", SocketColor);
        AddLegendRow(grid, 2, "✦ Bonus:", "Applies Soothing Balm to all parts", BalmColor);

        content.Widgets.Add(grid);

        // Note about healing pool
        content.Widgets.Add(new Label("small")
        {
            Text = $"Healing pool: {item.GetStatValue(Defs.Stats.HealingValue)} HP total",
            TextColor = new Color(100, 100, 100),
            Margin = new Thickness(0, 6, 0, 0),
            Wrap = true
        });

        legendPanel.Widgets.Add(content);
        return legendPanel;
    }

    private static void AddLegendRow(Grid grid, int row, string key, string value, Color valueColor)
    {
        var keyLabel = new Label("small")
        {
            Text = key,
            TextColor = new Color(150, 150, 150)
        };
        Grid.SetColumn(keyLabel, 0);
        Grid.SetRow(keyLabel, row);
        grid.Widgets.Add(keyLabel);

        var valueLabel = new Label("small")
        {
            Text = value,
            TextColor = valueColor
        };
        Grid.SetColumn(valueLabel, 1);
        Grid.SetRow(valueLabel, row);
        grid.Widgets.Add(valueLabel);
    }

    private void ApplyToPart(BodyPart bodyPart, int duration)
    {
        if (_healAmount <= 0)
        {
            return;
        }

        // Heal this part and apply soothing balm
        _healAmount -= UpdateHealth(bodyPart);
        bodyPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration, SoothingBalmPower));

        // Heal internal parts (bone, flesh, skin, organs)
        foreach (var internalPart in bodyPart.InternalParts)
        {
            _healAmount -= UpdateHealth(internalPart);
            internalPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration, SoothingBalmPower));
        }

        // Recursively apply to external parts (travels through sockets)
        foreach (var externalPart in bodyPart.ExternalParts)
        {
            ApplyToPart(externalPart, duration);
        }
    }

    private double UpdateHealth(BodyPart bodyPart)
    {
        var currentHealth = bodyPart.HitPoints;
        bodyPart.HitPoints += Math.Min(bodyPart.MaxHitPoints - bodyPart.HitPoints, _healAmount);
        return bodyPart.HitPoints - currentHealth;
    }
}
