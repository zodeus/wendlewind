namespace Grafted.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class ArterialThreadsHandler : MedicinalHandler
{
    private static readonly Color ArteryColor = new(180, 60, 60);        // Red for arteries
    private static readonly Color ThreadColor = new(200, 180, 140);      // Tan/gold for threads
    private static readonly Color SuccessColor = new(130, 200, 130);     // Green for healed
    private static readonly Color DamagedColor = new(200, 80, 80);       // Bright red for damaged

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        foreach (var internalPart in part.InternalParts)
        {
            if (internalPart.Type == BodyPartType.Artery && internalPart.HealthPercent < 1)
            {
                internalPart.HitPoints = internalPart.MaxHitPoints;
                return true;
            }
        }

        return false;
    }

    public override Widget? GetInfoPanel(Item item)
    {
        var panel = new VerticalStackPanel
        {
            Padding = new Thickness(20),
            MinWidth = 300,
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
            MaxWidth = 200
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
        infoContent.Widgets.Add(CreateInfoRow("1.", "Apply to body part with artery", ArteryColor));

        // Visual showing artery repair
        var visualRow = new HorizontalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4)
        };
        visualRow.Widgets.Add(CreateStatusBox("Damaged", DamagedColor));
        visualRow.Widgets.Add(new Label("small") { Text = "+", TextColor = ThreadColor, VerticalAlignment = VerticalAlignment.Center });
        visualRow.Widgets.Add(CreateStatusBox("Threads", ThreadColor));
        visualRow.Widgets.Add(new Label("small") { Text = "=", TextColor = SuccessColor, VerticalAlignment = VerticalAlignment.Center });
        visualRow.Widgets.Add(CreateStatusBox("Healed", SuccessColor));
        infoContent.Widgets.Add(visualRow);

        // Step 2: Full repair
        infoContent.Widgets.Add(CreateInfoRow("2.", "Fully repairs the artery", SuccessColor));

        infoContainer.Widgets.Add(infoContent);
        panel.Widgets.Add(infoContainer);

        // Note about targeting
        var notePanel = new Panel
        {
            Background = new SolidBrush(new Color(30, 30, 35)),
            Padding = new Thickness(10)
        };
        notePanel.Widgets.Add(new Label("small")
        {
            Text = "✓ Targets arteries specifically\n✗ Does not heal other internal parts",
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

    private static Widget CreateStatusBox(string label, Color color)
    {
        return new Panel
        {
            Background = new SolidBrush(color),
            Padding = new Thickness(6, 3),
            Widgets =
            {
                new Label("small")
                {
                    Text = label,
                    TextColor = Color.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Scale = new Vector2(0.85f, 0.85f)
                }
            }
        };
    }
}