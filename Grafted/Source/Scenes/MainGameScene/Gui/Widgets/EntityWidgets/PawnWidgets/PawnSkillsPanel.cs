namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnSkillsPanel : HorizontalStackPanel, IUpdatable {
    private readonly Dictionary<Skill, SkillPanelRow> _skillList = new();

    public PawnSkillsPanel(PawnSkills skills) {
        Spacing = 20;
        Padding = new Thickness(15);
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        var combatSkills = new Grid {
            RowSpacing = 15,
            ColumnSpacing = 25,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto
        };
        combatSkills.Widgets.Add(new Label { Text = "Skills", GridRow = 0, GridColumn = 0 });
        combatSkills.Widgets.Add(new Label { Text = "LVL", GridRow = 0, GridColumn = 1 });
        combatSkills.Widgets.Add(new Label { Text = "XP", GridRow = 0, GridColumn = 2, HorizontalAlignment = HorizontalAlignment.Center });
        int gridRow = 1;
        foreach (Skill skill in skills.Where(skill => skill.SkillType == SkillType.Combat).OrderBy(skill => skill.Def.Label)) {
            if (skill.TotalXp == 0) {
                continue;
            }

            _skillList[skill] = new SkillPanelRow(skill, combatSkills, gridRow++);
        }

        Widgets.Add(combatSkills);
        Update();
    }

    public void Update() {
        foreach ((Skill _, SkillPanelRow panel) in _skillList) {
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
                GridRow = gridRow, GridColumn = 1, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            _xp = new HorizontalProgressBar(BaseContent.Styles.Bar.Xp) {
                Width = 100, Height = 30,
                GridRow = gridRow, GridColumn = 2, VerticalAlignment = VerticalAlignment.Center
            };
            grid.Widgets.Add(new Label {
                Text = skill.Def.Label, GridRow = gridRow, GridColumn = 0, VerticalAlignment = VerticalAlignment.Center
            });
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