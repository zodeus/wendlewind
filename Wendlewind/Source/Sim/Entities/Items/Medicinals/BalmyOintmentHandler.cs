namespace Wendlewind.Sim.Entities.Items.Medicinals;

[UsedImplicitly]
public class BalmyOintmentHandler : MedicinalHandler
{
    private static readonly Color BalmColor = new(180, 200, 140);        // Soft green for soothing
    private static readonly Color PartColor = new(180, 150, 130);        // Flesh tone
    private static readonly Color InternalColor = new(140, 120, 110);    // Darker for internal
    private static readonly Color EffectColor = new(220, 200, 100);      // Golden glow effect
    private const double SoothingBalmPower = 1;

    public override bool ApplyToPart(Item item, BodyPart part)
    {
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        part.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration, 1));
        RemoveBurningAndAcid(part);
        foreach (var internalPart in part.AllInternalParts)
        {
            internalPart.TryAddModifier(BodyPartModifierGenerator.Generate(Defs.BodyPartModifiers.SoothingBalm, duration, SoothingBalmPower));
            RemoveBurningAndAcid(internalPart);
        }


        return true;
    }

    private void RemoveBurningAndAcid(BodyPart part)
    {
        foreach (var modifier in part.Modifiers.ToList())
        {
            if (modifier.Def == Defs.BodyPartModifiers.Burning || modifier.Def == Defs.BodyPartModifiers.Acid)
            {
                modifier.IsExpired = true;
                part.Modifiers.Remove(modifier);
            }
        }
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

        // Duration info
        var duration = item.ItemDef.MedicinalProperties!.DurationInTicks;
        panel.Widgets.Add(new Label("small")
        {
            Text = $"Duration: {duration} ticks",
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

        var infoContainer = new Panel
        {
            Background = new SolidBrush(new Color(25, 25, 30)),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var infoContent = new VerticalStackPanel { Spacing = 8 };

        // Step 1: Apply
        infoContent.Widgets.Add(CreateInfoRow("1.", "Apply to any body part", PartColor));

        // Visual showing coverage
        var visualContainer = new Panel
        {
            Background = new SolidBrush(new Color(35, 35, 40)),
            Padding = new Thickness(10),
            Margin = new Thickness(10, 4, 10, 4)
        };

        var visualColumn = new VerticalStackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        visualColumn.Widgets.Add(CreatePartWithBalm("Main Part", PartColor));

        var internalRow = new HorizontalStackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };
        internalRow.Widgets.Add(CreatePartWithBalm("Internal", InternalColor));
        internalRow.Widgets.Add(CreatePartWithBalm("Parts", InternalColor));
        visualColumn.Widgets.Add(internalRow);

        visualColumn.Widgets.Add(new Label("small")
        {
            Text = "All get Soothing Balm",
            TextColor = EffectColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        });

        visualContainer.Widgets.Add(visualColumn);
        infoContent.Widgets.Add(visualContainer);

        // Step 2: Effect
        infoContent.Widgets.Add(CreateInfoRow("2.", "Applies Soothing Balm modifier", BalmColor));

        // Step 3: Coverage
        infoContent.Widgets.Add(CreateInfoRow("3.", "Affects part and all internals", EffectColor));

        infoContainer.Widgets.Add(infoContent);
        panel.Widgets.Add(infoContainer);

        // Note
        panel.Widgets.Add(new Label("small")
        {
            Text = "✓ Always succeeds when applied",
            TextColor = new Color(130, 200, 130),
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

    private static Widget CreatePartWithBalm(string label, Color baseColor)
    {
        var container = new Panel
        {
            Background = new SolidBrush(baseColor),
            Padding = new Thickness(6, 3)
        };

        var row = new HorizontalStackPanel { Spacing = 4 };
        row.Widgets.Add(new Label("small")
        {
            Text = label,
            TextColor = Color.White,
            Scale = new Vector2(0.85f, 0.85f)
        });
        row.Widgets.Add(new Label("small")
        {
            Text = "✦",
            TextColor = new Color(180, 200, 140),
            Scale = new Vector2(0.8f, 0.8f)
        });

        container.Widgets.Add(row);
        return container;
    }
}