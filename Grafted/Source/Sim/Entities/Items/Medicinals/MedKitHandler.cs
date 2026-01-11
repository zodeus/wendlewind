namespace Grafted.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class MedKitHandler : MedicinalHandler
{
    private static readonly Color FleshColor = new(180, 120, 120);       // Pinkish for flesh
    private static readonly Color BoneColor = new(200, 200, 210);        // Light grayish-blue for bone
    private static readonly Color SkinColor = new(210, 180, 150);        // Tan for skin
    private static readonly Color OrganColor = new(140, 80, 80);         // Dark red for organs
    private static readonly Color SuccessColor = new(130, 200, 130);     // Green for healed
    private static readonly Color WarningColor = new(200, 160, 80);      // Orange for warnings

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        if (part.HealthPercent >= 1 && part.AllInternalParts.Any(p => p.HealthPercent < 1) == false)
        {
            return false;
        }

        part.HitPoints = part.MaxHitPoints;
        foreach (BodyPart internalPart in part.AllInternalParts)
        {
            internalPart.HitPoints = internalPart.MaxHitPoints;
        }

        return true;
    }

    public override Widget? GetInfoPanel(Item item)
    {
        var panel = new VerticalStackPanel
        {
            Padding = new Thickness(20),
            MinWidth = 320,
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
            MaxWidth = 220
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

        // How It Works section
        panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 12, 0, 8) });

        panel.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = "How It Works",
            TextColor = BaseContent.Colors.Text.Golden,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var infoContainer = new Panel
        {
            Background = new SolidBrush(new Color(25, 25, 30)),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var infoContent = new VerticalStackPanel { Spacing = 8 };

        // Step 1: Target
        infoContent.Widgets.Add(CreateInfoRow("1.", "Apply to damaged body part", FleshColor));

        // Visual showing what gets healed
        var visualContainer = new Panel
        {
            Background = new SolidBrush(new Color(35, 35, 40)),
            Padding = new Thickness(10),
            Margin = new Thickness(10, 4, 10, 4)
        };

        var visualContent = new VerticalStackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };

        visualContent.Widgets.Add(new Label("small")
        {
            Text = "Fully Heals:",
            TextColor = SuccessColor,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var partsRow = new HorizontalStackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        partsRow.Widgets.Add(CreatePartBox("Flesh", FleshColor));
        partsRow.Widgets.Add(CreatePartBox("Bone", BoneColor));
        partsRow.Widgets.Add(CreatePartBox("Skin", SkinColor));
        partsRow.Widgets.Add(CreatePartBox("Organ", OrganColor));
        visualContent.Widgets.Add(partsRow);

        visualContent.Widgets.Add(new Label("small")
        {
            Text = "→ 100% HP",
            TextColor = SuccessColor,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        visualContainer.Widgets.Add(visualContent);
        infoContent.Widgets.Add(visualContainer);

        // Step 2: Full heal
        infoContent.Widgets.Add(CreateInfoRow("2.", "Restores part to full health", SuccessColor));

        // Step 3: Single part
        infoContent.Widgets.Add(CreateInfoRow("3.", "Only affects one body part", WarningColor));

        infoContainer.Widgets.Add(infoContent);
        panel.Widgets.Add(infoContainer);

        // Notes
        var notePanel = new Panel
        {
            Background = new SolidBrush(new Color(30, 30, 35)),
            Padding = new Thickness(10)
        };
        notePanel.Widgets.Add(new Label("small")
        {
            Text = "✓ Heals ALL internal parts\n✗ Does not spread to other body parts",
            TextColor = new Color(150, 150, 150),
            Wrap = true
        });
        panel.Widgets.Add(notePanel);

        return panel;
    }

    private static HorizontalStackPanel CreateInfoRow(string stepNum, string text, Color textColor)
    {
        return new HorizontalStackPanel
        {
            Spacing = 8,
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
                    Scale = new Vector2(0.8f, 0.8f)
                }
            }
        };
    }
}