using System.Globalization;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Pawns.Modifiers;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.BodyPartPanelWidget;

public sealed class BodyPartPanel : EntityPanelBase
{
    private readonly BodyPart _bodyPart;
    private readonly BodyPartPanelModifiersLabel _modifiersPanel;

    public BodyPartPanel(BaseGui gui, BodyPart bodyPart, EntityPanelProperties? properties = null) : base(gui, bodyPart, properties)
    {
        _bodyPart = bodyPart;
        Padding = new Thickness(20);
        MinWidth = 300;

        _modifiersPanel = new BodyPartPanelModifiersLabel(bodyPart);

        VerticalStackPanel leftPanel = new() { Spacing = 5, MinWidth = 330 };
        leftPanel.Widgets.Add(new BodyPartPanelHealthLabel(bodyPart));
        leftPanel.Widgets.Add(_modifiersPanel);
        leftPanel.Widgets.Add(new BodyPartPanelBleedingLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelBrokenBonesLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelMobilityLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelFunctionalLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelArteryLabel(bodyPart));
        leftPanel.Widgets.Add(new BodyPartPanelDestroyedLabel(bodyPart));
        RegisterAttachedParts(gui, leftPanel, bodyPart);

        var rightPanel = new VerticalStackPanel { Spacing = 5 };

        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = bodyPart.Def.Description, Wrap = true });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Short Label: {bodyPart.LabelShort}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Type: {bodyPart.Type}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Size: {bodyPart.BloodAmount}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Attack Speed Mod: {bodyPart.AttackSpeedModifier}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Position: {bodyPart.Position}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is External: {bodyPart.IsExternal}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Attached To: {bodyPart.Socket?.Label ?? "n/a"}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Has Bones: {bodyPart.HasBones}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Equipment Slots: {string.Join(",", bodyPart.EquipmentSlots?.Select(s => s.ToString()) ?? new List<string>())}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Equipment: {string.Join(",", bodyPart.Equipment.Values.Select(i => i?.Label))}" });
        rightPanel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 15, 0, 15) });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Bone: {bodyPart.IsBone}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Severed: {bodyPart.IsSevered}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Vital: {bodyPart.IsVital}" });
        rightPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Ticks Since Last Hit: {bodyPart.TicksSinceLastHit}" });

        rightPanel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 15, 0, 15) });
        foreach (var baseStat in bodyPart.Def.BaseStats)
        {
            var row = new HorizontalStackPanel { Spacing = 10 };
            row.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"{baseStat.Def.Label}:" });
            row.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = bodyPart.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
            rightPanel.Widgets.Add(row);
        }

        var damageButton = new Button
        {
            Content = new Label
            {
                Text = "Damage", Margin = new Thickness(10, 10, 10, 10)
            }
        };
        damageButton.Click += (s, e) => bodyPart.HitPoints -= 10;
        rightPanel.Widgets.Add(damageButton);

        Widgets.Add(new HorizontalStackPanel
        {
            Spacing = 30, Widgets =
            {
                leftPanel, rightPanel
            }
        });
    }

    private void RegisterAttachedParts(BaseGui gui, VerticalStackPanel panel, BodyPart bodyPart)
    {
        if (bodyPart.Socket?.ParentPart != null)
        {
            var partPanel = new BodyPartPanelPartLabel(gui, bodyPart.Socket.ParentPart);
            panel.Widgets.Add(new Label { Text = "Parent", Margin = new Thickness(0, 20, 0, 0) });
            panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 5) });
            panel.Widgets.Add(partPanel);
        }

        if (bodyPart.ExternalParts.Any())
        {
            panel.Widgets.Add(new Label { Text = "External Parts", Margin = new Thickness(0, 20, 0, 0) });
            panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 5) });

            foreach (var externalPart in bodyPart.ExternalParts)
            {
                var partPanel = new BodyPartPanelPartLabel(gui, externalPart)
                {
                    Margin = new Thickness(0, 0, 0, 0)
                };
                panel.Widgets.Add(partPanel);
            }
        }

        if (bodyPart.InternalParts.Any())
        {
            panel.Widgets.Add(new Label { Text = "Internal Parts", Margin = new Thickness(0, 20, 0, 0) });
            panel.Widgets.Add(new HorizontalSeparator { Margin = new Thickness(0, 5, 0, 5) });
            foreach (BodyPart internalPart in bodyPart.InternalParts)
            {
                var partPanel = new BodyPartPanelPartLabel(gui, internalPart)
                {
                    Margin = new Thickness(0, 0, 0, 0)
                };
                panel.Widgets.Add(partPanel);
            }
        }
    }

    public override void Update()
    {
        _modifiersPanel.Update();
    }
}

public sealed class BodyPartPanelModifiersLabel : VerticalStackPanel
{
    private Dictionary<BodyPartModifier, Label> _labels = new();

    public BodyPartPanelModifiersLabel(BodyPart bodyPart)
    {
        Spacing = 5;
        foreach (var modifier in bodyPart.Modifiers)
        {
            var label = CreateLabel(modifier);
            Widgets.Add(label);
            _labels.Add(modifier, label);
        }

        bodyPart.ModifiersChanged += Test;
    }

    private static Label CreateLabel(BodyPartModifier modifier)
    {
        var label = new Label
        {
            Text = GetLabelText(modifier),
            Padding = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.SimpleWhite], modifier.Def.Color),
            TextColor = modifier.Def.Color
        };
        return label;
    }

    private void Test(BodyPartModifier mod, BodyPartModifierEventType type)
    {
        switch (type)
        {
            case BodyPartModifierEventType.Added:
                var label = CreateLabel(mod);
                Widgets.Add(label);
                _labels.Add(mod, label);
                break;
            case BodyPartModifierEventType.Removed:
                _labels[mod].RemoveFromParent();
                _labels.Remove(mod);
                break;
        }
    }

    private static string GetLabelText(BodyPartModifier modifier)
    {
        var timeRemaining = modifier.DurationInTicks == 0 ? "\u221e" : modifier.TicksRemaining + "t";
        return $"{modifier.Label} {timeRemaining}";
    }

    public void Update()
    {
        foreach (var pair in _labels)
        {
            var (modifier, label) = pair;
            label.Text = GetLabelText(modifier);
        }
    }
}