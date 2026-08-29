namespace Wendlewind.Sim.Entities.Items.Medicinals;

/// <summary>
/// Handler for StrengthenBones - increases the max HP of all bones by 3%
/// and fully heals them, regardless of which part is clicked.
/// </summary>
[UsedImplicitly]
public class StrengthenBonesHandler : MedicinalHandler
{
    private const double MaxHpIncreasePercent = 0.05; // 5% increase

    private static readonly Color BoneColor = new(200, 200, 210);           // Light grayish-blue for bone
    private static readonly Color StrengthColor = new(220, 180, 100);       // Golden for strengthening effect
    private static readonly Color HealColor = new(130, 200, 130);           // Green for healing
    private static readonly Color BoostColor = new(180, 140, 220);          // Purple for max HP boost

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var body = part.Body;
        if (body == null) return false;

        // Find all bone parts in the entire body
        var boneParts = body.AllParts
            .Where(p => p.Substance == SubstanceType.Bone)
            .ToList();

        if (boneParts.Count == 0) return false;

        foreach (var bonePart in boneParts)
        {
            // Increase max HP by 5%
            var hpIncrease = bonePart.MaxHitPoints * MaxHpIncreasePercent;
            bonePart.MaxHitPoints += hpIncrease;

            // Fully heal the bone
            bonePart.HitPoints = bonePart.MaxHitPoints;
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
            Text = "Strengthens ALL Bones:",
            TextColor = StrengthColor,
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

        // Step 2: Boost max HP
        infoContent.Widgets.Add(CreateInfoRow("2.", $"Increases bone max HP by {MaxHpIncreasePercent * 100}%", BoostColor));

        // Step 3: Full heal
        infoContent.Widgets.Add(CreateInfoRow("3.", "Fully heals all bones to new max", HealColor));

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
            Text = "✓ Permanent max HP increase\n✓ Works on entire skeleton\n✓ Click anywhere - affects all bones\n✗ Does not affect flesh, organs, or skin",
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
