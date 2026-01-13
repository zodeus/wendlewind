namespace Grafted.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class AntiNecroticSerumHandler : MedicinalHandler
{
    private static readonly Color NecrosisColor = new(80, 60, 80);       // Dark purple for necrosis
    private static readonly Color SerumColor = new(100, 180, 220);       // Light blue for the serum
    private static readonly Color WarningColor = new(200, 120, 80);      // Orange for warnings
    private static readonly Color SuccessColor = new(130, 200, 130);     // Green for success

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        if (part.HasModifier(Defs.BodyPartModifiers.Necrosis) && part.HasModifier(Defs.BodyPartModifiers.NecrosisSerum) == false)
        {
            part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.NecrosisSerum, duration, 1));
            return true;
        }

        return false;
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

        // Duration info
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        panel.Widgets.Add(new Label("small")
        {
            Text = $"Duration: {duration} ticks",
            TextColor = SerumColor
        });

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

        // Step 1: Targets necrosis
        infoContent.Widgets.Add(CreateInfoRow("1.", "Apply to necrotic body part", NecrosisColor));

        // Visual showing necrosis → cured
        var visualRow = new HorizontalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4)
        };
        visualRow.Widgets.Add(CreateStatusBox("Necrosis", NecrosisColor));
        visualRow.Widgets.Add(new Label("small") { Text = "→", TextColor = SerumColor, VerticalAlignment = VerticalAlignment.Center });
        visualRow.Widgets.Add(CreateStatusBox("Treating", SerumColor));
        visualRow.Widgets.Add(new Label("small") { Text = "→", TextColor = SuccessColor, VerticalAlignment = VerticalAlignment.Center });
        visualRow.Widgets.Add(CreateStatusBox("Cured", SuccessColor));
        infoContent.Widgets.Add(visualRow);

        // Step 2: Treatment period
        infoContent.Widgets.Add(CreateInfoRow("2.", "Treatment takes time to complete", SerumColor));

        // Step 3: Cures necrosis
        infoContent.Widgets.Add(CreateInfoRow("3.", "Cures necrosis when treatment ends", SuccessColor));

        infoContainer.Widgets.Add(infoContent);
        panel.Widgets.Add(infoContainer);

        // Requirements note
        panel.Widgets.Add(new Label("small")
        {
            Text = "⚠ Only works on parts with active necrosis",
            TextColor = WarningColor,
            Wrap = true
        });

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

    private static Widget CreateStatusBox(string label, Color color)
    {
        return new Panel
        {
            Background = new SolidBrush(color),
            Padding = new Thickness(8, 4),
            Widgets =
            {
                new Label("small")
                {
                    Text = label,
                    TextColor = Color.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
        };
    }
}