using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Grafted.Sim.Entities;
using Grafted.Sim.Entities.Items;
using Grafted.Sim.Entities.Pawns;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class BodyPartPanel : EntityPanelBase {
    private readonly BodyPart _bodyPart;

    public BodyPartPanel(BodyPart bodyPart, EntityPanelProperties? properties = null) : base(bodyPart, properties) {
        _bodyPart = bodyPart;
        Padding = new Thickness(20);
        MinWidth = 300;

        HorizontalStackPanel panel = new() { Spacing = 30 };
        VerticalStackPanel leftPanel = new() { Spacing = 5 };
        panel.AddChild(leftPanel);
        leftPanel.AddChild(new Image { Background = new TextureRegion(bodyPart.Icon), Width = 48, Height = 48 });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = bodyPart.Def.Description, Wrap = true, Margin = new Thickness(10) });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Type: {bodyPart.Type}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Size: {bodyPart.Size}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Is External: {bodyPart.IsExternal}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Attached To: {bodyPart.Socket?.Label ?? "n/a"}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Has Bones: {bodyPart.HasBones}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Equipment Slots: {string.Join(",", bodyPart.EquipmentSlots?.Select(s => s.ToString()) ?? new List<string>())}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Equipment: {string.Join(",", bodyPart.Equipment.Values.Select(i => i?.Label))}" });
        leftPanel.AddChild(new HorizontalSeparator());
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Health %: {bodyPart.HealthPercent}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Hit Points: {bodyPart.HitPoints}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Has Mobility: {bodyPart.HasMobility}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Bleeding: {bodyPart.IsBleeding}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Bone: {bodyPart.IsBone}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Destroyed: {bodyPart.IsDestroyed}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Functional: {bodyPart.IsFunctional}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Severed: {bodyPart.IsSevered}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Has Broken Bones: {bodyPart.HasBrokenBones}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Is Artery Functional: {bodyPart.IsArteryFunctional}" });
        leftPanel.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"Ticks Since Last Hit: {bodyPart.TicksSinceLastHit}" });
        leftPanel.AddChild(new HorizontalSeparator());
        foreach (BaseStat baseStat in bodyPart.Def.BaseStats) {
            var row = new HorizontalStackPanel { Spacing = 10 };
            row.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = $"{baseStat.Def.Label}:" });
            row.AddChild(new Label(BaseContent.Styles.Label.Small) { Text = bodyPart.GetStatValue(baseStat.Def).ToString(CultureInfo.InvariantCulture) });
            leftPanel.AddChild(row);
        }

        var rightPanel = new VerticalStackPanel { Spacing = 5 };
        panel.AddChild(rightPanel);
        if (bodyPart.InternalParts.Any()) {
            VerticalStackPanel internalPartsPanel = new() { Spacing = 5};
            internalPartsPanel.AddChild(new Label() { Text = "Internal Parts" });
            foreach (BodyPart internalPart in bodyPart.InternalParts) {
                Image image = new() { Background = new TextureRegion(internalPart.Icon), Width = 32, Height = 32 };
                image.TouchDown += (_, _) => Core.Sim.Gui!.ViewEntity(internalPart);
                internalPartsPanel.AddChild(new HorizontalStackPanel {
                    Spacing = 10, Widgets = {
                        image, new Label(BaseContent.Styles.Label.Small) { Text = internalPart.Label, VerticalAlignment = VerticalAlignment.Center }
                    }
                });
            }

            rightPanel.AddChild(internalPartsPanel);
        }

        rightPanel.AddChild(new HorizontalSeparator());
        if (bodyPart.ExternalParts.Any()) {
            VerticalStackPanel externalPartsPanel = new() { Spacing = 5 };
            externalPartsPanel.AddChild(new Label() { Text = "External Parts" });
            foreach (BodyPart externalPart in bodyPart.ExternalParts) {
                Image image = new() { Background = new TextureRegion(externalPart.Icon), Width = 32, Height = 32 };
                image.TouchDown += (_, _) => Core.Sim.Gui!.ViewEntity(externalPart);
                externalPartsPanel.AddChild(new HorizontalStackPanel {
                    Spacing = 10, Widgets = {
                        image, new Label(BaseContent.Styles.Label.Small) { Text = externalPart.Label, VerticalAlignment = VerticalAlignment.Center }
                    }
                });
            }

            rightPanel.AddChild(externalPartsPanel);
        }

        AddChild(panel);
    }

    public override void Update() { }
}