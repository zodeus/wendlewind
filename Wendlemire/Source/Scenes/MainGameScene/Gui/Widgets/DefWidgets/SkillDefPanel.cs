﻿namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.DefWidgets;

public class SkillDefPanel : DefPanelBase {
    public SkillDefPanel(SkillDef skill, DefPanelProperties? properties = null) : base(skill, properties) {
        MinWidth = 200;
        Spacing = 5;
        Widgets.Add(new Label { Text = $"Skill Type: {skill.SkillType}" });
    }
}