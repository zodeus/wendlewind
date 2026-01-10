using System.Globalization;

namespace Grafted.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class MendersMistHandler : MedicinalHandler
{
    private double _mistAmount;

    // Colors for the infographic
    private static readonly Color BoneColor = new(200, 200, 210);      // Light grayish-blue for bone
    private static readonly Color FleshColor = new(180, 120, 120);     // Pinkish for flesh
    private static readonly Color SkinColor = new(210, 180, 150);      // Tan for skin
    private static readonly Color OrganColor = new(100, 60, 60);       // Dark red for organs (not healed)
    private static readonly Color SocketColor = new(160, 140, 100);    // Gold-ish for socket connections
    private static readonly Color FlowArrowColor = new(100, 180, 100); // Green for flow indicators
    private static readonly Color HealedTextColor = new(130, 200, 130);
    private static readonly Color NotHealedTextColor = new(180, 100, 100);

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var healingValue = item.GetStatValue(Defs.Stats.HealingValue);
        _mistAmount = healingValue;
        MistPart(part);
        return _mistAmount < healingValue;
    }

    public override Widget? GetInfoPanel(Item item)
    {
        var panel = new VerticalStackPanel
        {
            Padding = new Thickness(20),
            MinWidth = 340,
            Spacing = 8,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]
        };

        // ═══════════════════════════════════════════════════════════════════
        // Header Section: Icon + Description
        // ═══════════════════════════════════════════════════════════════════
        var headerSection = new HorizontalStackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Icon with frame
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

        // Description
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

        // ═══════════════════════════════════════════════════════════════════
        // Infographic Section: How It Works
        // ═══════════════════════════════════════════════════════════════════
        panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 12, 0, 8) });

        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "How It Works",
            TextColor = BaseContent.Colors.Text.Golden,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Create the infographic visualization
        panel.Widgets.Add(CreateInfographic());

        // ═══════════════════════════════════════════════════════════════════
        // Legend Section
        // ═══════════════════════════════════════════════════════════════════
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

        // Step 1: Apply to body part
        content.Widgets.Add(CreateStepRow("1.", "Apply to body part", FlowArrowColor));

        // Visual: Main body part with internal parts
        content.Widgets.Add(CreateBodyPartVisual());

        // Step 2: Heals through sockets
        content.Widgets.Add(CreateStepRow("2.", "Spreads through sockets", SocketColor));

        // Visual: Flow arrows
        content.Widgets.Add(CreateFlowVisual());

        // Step 3: What it heals
        content.Widgets.Add(CreateStepRow("3.", "Heals until depleted", HealedTextColor));

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

    private static Widget CreateBodyPartVisual()
    {
        var container = new Panel
        {
            Background = new SolidBrush(new Color(35, 35, 40)),
            Padding = new Thickness(12),
            Margin = new Thickness(20, 4, 20, 4)
        };

        var row = new HorizontalStackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };

        // Main body part (Flesh) - healed
        row.Widgets.Add(CreatePartBox("Flesh", FleshColor, true));

        // Internal parts group
        var internalGroup = new VerticalStackPanel { Spacing = 4 };
        internalGroup.Widgets.Add(new Label("small")
        {
            Text = "Internal:",
            TextColor = new Color(120, 120, 120),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var internalRow = new HorizontalStackPanel { Spacing = 4 };
        internalRow.Widgets.Add(CreatePartBox("Bone", BoneColor, true));
        internalRow.Widgets.Add(CreatePartBox("Skin", SkinColor, true));
        internalRow.Widgets.Add(CreatePartBox("Organ", OrganColor, false));
        internalGroup.Widgets.Add(internalRow);

        row.Widgets.Add(internalGroup);

        container.Widgets.Add(row);
        return container;
    }

    private static Widget CreatePartBox(string label, Color color, bool isHealed)
    {
        var box = new Panel
        {
            Background = new SolidBrush(color),
            Padding = new Thickness(6, 3),
            Width = 50,
            Height = 28
        };

        var content = new VerticalStackPanel { VerticalAlignment = VerticalAlignment.Center };
        content.Widgets.Add(new Label("small")
        {
            Text = label,
            TextColor = isHealed ? Color.White : new Color(150, 150, 150),
            HorizontalAlignment = HorizontalAlignment.Center,
            Scale = new Vector2(0.8f, 0.8f)
        });

        if (!isHealed)
        {
            content.Widgets.Add(new Label("small")
            {
                Text = "✗",
                TextColor = NotHealedTextColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                Scale = new Vector2(0.7f, 0.7f)
            });
        }

        box.Widgets.Add(content);
        return box;
    }

    private static Widget CreateFlowVisual()
    {
        var container = new Panel
        {
            Background = new SolidBrush(new Color(35, 35, 40)),
            Padding = new Thickness(8, 6),
            Margin = new Thickness(20, 0, 20, 4)
        };

        var row = new HorizontalStackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        row.Widgets.Add(CreateFlowNode("Torso", FleshColor));
        row.Widgets.Add(CreateArrow());
        row.Widgets.Add(CreateFlowNode("Socket", SocketColor));
        row.Widgets.Add(CreateArrow());
        row.Widgets.Add(CreateFlowNode("Arm", FleshColor));
        row.Widgets.Add(CreateArrow());
        row.Widgets.Add(CreateFlowNode("Socket", SocketColor));
        row.Widgets.Add(CreateArrow());
        row.Widgets.Add(CreateFlowNode("Hand", FleshColor));

        container.Widgets.Add(row);
        return container;
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

        AddLegendRow(grid, 0, "✓ Heals:", "Bone, Flesh, Skin", HealedTextColor);
        AddLegendRow(grid, 1, "✗ Ignores:", "Organs, Arteries", NotHealedTextColor);
        AddLegendRow(grid, 2, "⟳ Spreads:", "Through sockets to external parts", SocketColor);

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

    private void MistPart(BodyPart bodyPart)
    {
        if (_mistAmount <= 0)
        {
            return;
        }

        _mistAmount -= UpdateHealth(bodyPart);
        foreach (var internalPart in bodyPart.InternalParts)
        {
            if (internalPart.Substance == SubstanceType.Bone || internalPart.Type is BodyPartType.Skin)
            {
                _mistAmount -= UpdateHealth(internalPart);
            }
        }

        foreach (var externalPart in bodyPart.ExternalParts)
        {
            MistPart(externalPart);
        }
    }

    private double UpdateHealth(BodyPart bodyPart)
    {
        var currentHealth = bodyPart.HitPoints;
        bodyPart.HitPoints += Math.Min(bodyPart.MaxHitPoints - bodyPart.HitPoints, _mistAmount);
        return bodyPart.HitPoints - currentHealth;
    }
}