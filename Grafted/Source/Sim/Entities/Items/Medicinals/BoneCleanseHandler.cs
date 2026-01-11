namespace Grafted.Sim.Entities.Items.Medicinals;

/// <summary>
/// Handler for BoneCleanse - removes all body part modifiers from all bones in the body
/// and restores 25% bone health, regardless of which part is clicked.
/// </summary>
[UsedImplicitly]
public class BoneCleanseHandler : MedicinalHandler
{
    private static readonly Color BoneColor = new(200, 200, 210);           // Light grayish-blue for bone
    private static readonly Color CleanseColor = new(180, 220, 240);        // Light cyan for cleansing effect
    private static readonly Color HealColor = new(130, 200, 130);           // Green for healing
    private static readonly Color WarningColor = new(200, 160, 80);         // Orange for warnings
    private static readonly Color ModifierColor = new(180, 100, 100);       // Red for modifiers being removed

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var body = part.Body;
        if (body == null) return false;

        // Find all bone parts in the entire body
        var boneParts = body.AllParts
            .Where(p => p.Substance == SubstanceType.Bone)
            .ToList();

        if (boneParts.Count == 0) return false;

        var anyEffect = false;

        foreach (var bonePart in boneParts)
        {
            // Remove all modifiers from this bone
            if (bonePart.Modifiers.Count > 0)
            {
                bonePart.Modifiers.Clear();
                anyEffect = true;
            }

            // Restore 25% bone health
            if (bonePart.HealthPercent < 1)
            {
                var healAmount = bonePart.MaxHitPoints * 0.25;
                bonePart.HitPoints = Math.Min(bonePart.MaxHitPoints, bonePart.HitPoints + healAmount);
                anyEffect = true;
            }
        }

        return anyEffect;
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

        // Step 1: Target any part
        infoContent.Widgets.Add(CreateInfoRow("1.", "Apply to any body part", BoneColor));

        // Visual showing effect scope
        var visualContainer = new Panel
        {
            Background = new SolidBrush(new Color(35, 35, 40)),
            Padding = new Thickness(10),
            Margin = new Thickness(10, 4, 10, 4)
        };

        var visualContent = new VerticalStackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };

        visualContent.Widgets.Add(new Label("small")
        {
            Text = "Affects ALL Bones:",
            TextColor = CleanseColor,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // Show bone representation
        var boneRow = new HorizontalStackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        boneRow.Widgets.Add(CreatePartBox("Skull", BoneColor));
        boneRow.Widgets.Add(CreatePartBox("Spine", BoneColor));
        boneRow.Widgets.Add(CreatePartBox("Ribs", BoneColor));
        boneRow.Widgets.Add(CreatePartBox("Limbs", BoneColor));
        visualContent.Widgets.Add(boneRow);

        visualContainer.Widgets.Add(visualContent);
        infoContent.Widgets.Add(visualContainer);

        // Step 2: Cleanse
        infoContent.Widgets.Add(CreateInfoRow("2.", "Removes ALL modifiers from bones", ModifierColor));

        // Step 3: Heal
        infoContent.Widgets.Add(CreateInfoRow("3.", "Restores +25% bone health", HealColor));

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
            Text = "✓ Works on entire skeleton\n✓ Click anywhere - affects all bones\n✗ Does not affect flesh, organs, or skin",
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
