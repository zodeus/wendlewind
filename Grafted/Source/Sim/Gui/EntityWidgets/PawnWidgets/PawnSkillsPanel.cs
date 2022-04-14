using System.Collections.Generic;
using System.Linq;
using Grafted.Sim.Entities.Pawns;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public class PawnSkillsPanel : HorizontalStackPanel {
    private readonly Dictionary<Skill, SkillPanelRow> _skillList = new();

    public PawnSkillsPanel(PawnSkills skills) {
        Spacing = 20;
        var combatSkills = new Grid {
            RowSpacing = 15,
            ColumnSpacing = 25,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto
        };
        combatSkills.AddChild(new Label(BaseContent.Styles.Label.Medium) { Text = "Arms Skills", GridRow = 0, GridColumn = 0, GridColumnSpan = 3 });
        combatSkills.AddChild(new Label() { Text = "LVL", GridRow = 1, GridColumn = 1 });
        combatSkills.AddChild(new Label() { Text = "XP", GridRow = 1, GridColumn = 2, HorizontalAlignment = HorizontalAlignment.Center });
        int gridRow = 2;
        foreach (Skill skill in skills.Where(skill => skill.SkillType == SkillType.Arms).OrderBy(skill => skill.Def.Label)) {
            _skillList[skill] = new SkillPanelRow(skill, combatSkills, gridRow++);
        }

        AddChild(combatSkills);
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
                Width = 100, Height = 10,
                GridRow = gridRow, GridColumn = 2, VerticalAlignment = VerticalAlignment.Center
            };
            grid.AddChild(new Label {
                Text = skill.Def.Label, GridRow = gridRow, GridColumn = 0, VerticalAlignment = VerticalAlignment.Center
            });
            grid.AddChild(_level);
            grid.AddChild(_xp);
        }

        public void Update() {
            if (_skill.TotalXp == 0) {
                return;
            }
            _level.Text = _skill.Level.ToString();
            _level.TextColor = Color.Lerp(Color.DarkRed, Color.YellowGreen, _skill.Level / 10f);
            _xp.Value = _skill.CurrentLevelXp / _skill.XpRequiredForLevelUp * 100;
        }
    }
}