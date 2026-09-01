namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public class PawnSkillsPanel : Panel, IUpdatable {
    private readonly Dictionary<Skill, SkillPanelRow> _skillList = new();

    public PawnSkillsPanel(PawnSkills skills) {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;

        var container = new VerticalStackPanel { Spacing = 4 };

        foreach (var skill in skills
                     .Where(skill => skill.SkillType == SkillType.Combat && skill.TotalXp > 0)
                     .OrderBy(skill => skill.Def.Label))
        {
            var row = new SkillPanelRow(skill);
            _skillList[skill] = row;
            container.Widgets.Add(row);
        }

        Widgets.Add(container);
        Update();
    }

    public void Update() {
        foreach ((var _, var panel) in _skillList) {
            panel.Update();
        }
    }

    private class SkillPanelRow : HorizontalStackPanel {
        private readonly Skill _skill;
        private readonly Label _levelLabel;
        private readonly HorizontalProgressBar _xpBar;
        private static readonly Color LevelBgColor = new(20, 20, 25, 200);

        public SkillPanelRow(Skill skill) {
            _skill = skill;
            Spacing = 6;
            VerticalAlignment = VerticalAlignment.Center;

            // Skill name label
            var skillLabel = new Label(BaseContent.Styles.Label.Small) {
                Text = skill.Def.Label,
                Width = 90,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Level display with background
            var levelPanel = new Panel {
                Width = 40,
                Height = 18,
                Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame],
                VerticalAlignment = VerticalAlignment.Center
            };

            _levelLabel = new Label(BaseContent.Styles.Label.Small) {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            levelPanel.Widgets.Add(_levelLabel);

            // Compact XP bar
            _xpBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Xp) {
                Width = 60,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center
            };

            Widgets.Add(skillLabel);
            Widgets.Add(levelPanel);
            Widgets.Add(_xpBar);
        }

        public void Update() {
            _levelLabel.Text = _skill.Level.ToString();
            _levelLabel.TextColor = Color.Lerp(new Color(180, 80, 80), new Color(120, 200, 80), _skill.Level / 10f);
            _xpBar.Value = _skill.CurrentLevelXp / _skill.XpRequiredForLevelUp * 100;
        }
    }
}