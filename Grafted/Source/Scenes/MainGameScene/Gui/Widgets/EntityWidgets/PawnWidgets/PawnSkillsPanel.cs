namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnSkillsPanel : HorizontalStackPanel, IUpdatable {
    private readonly Dictionary<Skill, SkillPanelRow> _skillList = new();

    public PawnSkillsPanel(PawnSkills skills) {
        Spacing = 20;
        Padding = new Thickness(15);
        HorizontalAlignment = HorizontalAlignment.Left;
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        var combatSkills = new Grid {
            RowSpacing = 15,
            ColumnSpacing = 25,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto
        };
        var skillsLabel = new Label { Text = "Skills" };
        Grid.SetRow(skillsLabel, 0); Grid.SetColumn(skillsLabel, 0);
        combatSkills.Widgets.Add(skillsLabel);
        
        var lvlLabel = new Label { Text = "LVL" };
        Grid.SetRow(lvlLabel, 0); Grid.SetColumn(lvlLabel, 1);
        combatSkills.Widgets.Add(lvlLabel);
        
        var xpLabel = new Label { Text = "XP", HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetRow(xpLabel, 0); Grid.SetColumn(xpLabel, 2);
        combatSkills.Widgets.Add(xpLabel);
        var gridRow = 1;
        foreach (var skill in skills.Where(skill => skill.SkillType == SkillType.Combat).OrderBy(skill => skill.Def.Label)) {
            if (skill.TotalXp == 0) {
                continue;
            }

            _skillList[skill] = new SkillPanelRow(skill, combatSkills, gridRow++);
        }

        Widgets.Add(combatSkills);
        Update();
    }

    public void Update() {
        foreach ((var _, var panel) in _skillList) {
            panel.Update();
        }
    }

    private class SkillPanelRow {
        private readonly Skill _skill;
        private readonly Label _level;
        private readonly HorizontalProgressBar _xp;

        public SkillPanelRow(Skill skill, Grid grid, int gridRow) {
            _skill = skill;
            _level = new Label(BaseContent.Styles.Label.Medium) {
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(_level, gridRow); Grid.SetColumn(_level, 1);
            
            _xp = new HorizontalProgressBar(BaseContent.Styles.Bar.Xp) {
                Width = 100, Height = 30,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(_xp, gridRow); Grid.SetColumn(_xp, 2);
            
            var skillLabel = new Label { Text = skill.Def.Label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(skillLabel, gridRow); Grid.SetColumn(skillLabel, 0);
            grid.Widgets.Add(skillLabel);
            grid.Widgets.Add(_level);
            grid.Widgets.Add(_xp);
        }

        public void Update() {
            _level.Text = _skill.Level.ToString();
            _level.TextColor = Color.Lerp(Color.DarkRed, Color.YellowGreen, _skill.Level / 10f);
            _xp.Value = _skill.CurrentLevelXp / _skill.XpRequiredForLevelUp * 100;
        }
    }
}