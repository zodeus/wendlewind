using System.Globalization;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class BodyPartPanel : EntityPanelBase
{
    private readonly BodyPart _bodyPart;
    private readonly Label _hitPoints;
    private readonly Label _healthPercent;
    private readonly Label _modifiers;

    public BodyPartPanel(BaseGui gui, BodyPart bodyPart, EntityPanelProperties? properties = null) : base(gui, bodyPart, properties)
    {
        _bodyPart = bodyPart;
        Padding = new Thickness(20);
        MinWidth = 300;

        _hitPoints = new Label(BaseContent.Styles.Label.Small);
        _healthPercent = new Label(BaseContent.Styles.Label.Small);
        _modifiers = new Label(BaseContent.Styles.Label.Small);
        HorizontalStackPanel panel = new() { Spacing = 30 };
        VerticalStackPanel leftPanel = new() { Spacing = 5 };
        panel.Widgets.Add(leftPanel);
        leftPanel.Widgets.Add(new Image { Background = new TextureRegion(bodyPart.Icon), Width = 48, Height = 48 });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = bodyPart.Def.Description, Wrap = true, Margin = new Thickness(10) });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Type: {bodyPart.Type}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Size: {bodyPart.Size}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Attack Speed Mod: {bodyPart.AttackSpeedModifier}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Position: {bodyPart.Position}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is External: {bodyPart.IsExternal}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Attached To: {bodyPart.Socket?.Label ?? "n/a"}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Has Bones: {bodyPart.HasBones}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Equipment Slots: {string.Join(",", bodyPart.EquipmentSlots?.Select(s => s.ToString()) ?? new List<string>())}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Equipment: {string.Join(",", bodyPart.Equipment.Values.Select(i => i?.Label))}" });
        leftPanel.Widgets.Add(new HorizontalSeparator());
        leftPanel.Widgets.Add(_healthPercent);
        leftPanel.Widgets.Add(_hitPoints);
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Max Hit Points: {bodyPart.MaxHitPoints}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Has Mobility: {bodyPart.HasMobility}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Bleeding: {bodyPart.IsBleeding}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Bone: {bodyPart.IsBone}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Destroyed: {bodyPart.IsDestroyed}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Functional: {bodyPart.IsFunctional}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Severed: {bodyPart.IsSevered}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Has Broken Bones: {bodyPart.HasBrokenBones}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Artery Functional: {bodyPart.IsArteryFunctional}" });
        leftPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"Ticks Since Last Hit: {bodyPart.TicksSinceLastHit}" });
        leftPanel.Widgets.Add(_modifiers);
        leftPanel.Widgets.Add(new HorizontalSeparator());
        foreach (BaseStat baseStat in bodyPart.Def.BaseStats)
        {
            var row = new HorizontalStackPanel { Spacing = 10 };
            row.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = $"{baseStat.Def.Label}:" });
            row.Widgets.Add(new Label(BaseContent.Styles.Label.Small) { Text = bodyPart.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
            leftPanel.Widgets.Add(row);
        }

        var rightPanel = new VerticalStackPanel { Spacing = 5 };
        panel.Widgets.Add(rightPanel);
        if (bodyPart.Socket?.ParentPart != null)
        {
            Image image = new() { Background = new TextureRegion(bodyPart.Socket.ParentPart.Icon), Width = 32, Height = 32 };
            image.TouchDown += (_, _) => Gui.ViewEntity(bodyPart.Socket.ParentPart);
            rightPanel.Widgets.Add(new HorizontalStackPanel
            {
                Spacing = 10, Widgets =
                {
                    new Label { Text = "Parent:", VerticalAlignment = VerticalAlignment.Center },
                    image, new Label { Text = bodyPart.Socket.ParentPart.Label, VerticalAlignment = VerticalAlignment.Center }
                }
            });
            rightPanel.Widgets.Add(new HorizontalSeparator());
        }

        if (bodyPart.InternalParts.Any())
        {
            VerticalStackPanel internalPartsPanel = new() { Spacing = 5 };
            internalPartsPanel.Widgets.Add(new Label { Text = "Internal Parts" });
            foreach (BodyPart internalPart in bodyPart.InternalParts)
            {
                Image image = new() { Background = new TextureRegion(internalPart.Icon), Width = 32, Height = 32 };
                image.TouchDown += (_, _) => Gui.ViewEntity(internalPart);
                internalPartsPanel.Widgets.Add(new HorizontalStackPanel
                {
                    Spacing = 10, Widgets =
                    {
                        image, new Label(BaseContent.Styles.Label.Small) { Text = internalPart.Label, VerticalAlignment = VerticalAlignment.Center }
                    }
                });
            }

            rightPanel.Widgets.Add(internalPartsPanel);
        }

        rightPanel.Widgets.Add(new HorizontalSeparator());
        if (bodyPart.ExternalParts.Any())
        {
            VerticalStackPanel externalPartsPanel = new() { Spacing = 5 };
            externalPartsPanel.Widgets.Add(new Label { Text = "External Parts" });
            foreach (BodyPart externalPart in bodyPart.ExternalParts)
            {
                Image image = new() { Background = new TextureRegion(externalPart.Icon), Width = 32, Height = 32 };
                image.TouchDown += (_, _) => Gui.ViewEntity(externalPart);
                externalPartsPanel.Widgets.Add(new HorizontalStackPanel
                {
                    Spacing = 10, Widgets =
                    {
                        image, new Label(BaseContent.Styles.Label.Small) { Text = externalPart.Label, VerticalAlignment = VerticalAlignment.Center }
                    }
                });
            }

            rightPanel.Widgets.Add(externalPartsPanel);
        }

        Widgets.Add(panel);
    }

    public override void Update()
    {
        _hitPoints.Text = $"Hit Point: {_bodyPart.HitPoints:0.00}";
        _healthPercent.Text = $"Health: {_bodyPart.HealthPercent:P}";
        _modifiers.Text = "Modifiers: " + string.Join(",", _bodyPart.Modifiers.Select(i => i.Label));
    }
}