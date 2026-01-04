using System.Globalization;
using SolidBrush = Myra.Graphics2D.Brushes.SolidBrush;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

/// <summary>
/// Combined panel for Skills, Stats, and Traits - replaces the separate tabs.
/// </summary>
public sealed class PawnProfilePanel : ScrollViewer, IUpdatable
{
    private readonly Pawn _pawn;
    private readonly Dictionary<Skill, SkillRow> _skillRows = new();

    public PawnProfilePanel(Pawn pawn)
    {
        _pawn = pawn;

        var content = new VerticalStackPanel
        {
            Spacing = 24,
            Padding = new Thickness(8, 4, 8, 20)
        };

        // === TRAITS SECTION ===
        if (pawn.Traits.Any())
        {
            content.Widgets.Add(CreateTraitsSection(pawn));
        }

        // === SKILLS SECTION ===
        var combatSkills = pawn.Skills.Where(s => s.SkillType == SkillType.Combat && s.TotalXp > 0).ToList();
        if (combatSkills.Any())
        {
            content.Widgets.Add(CreateSkillsSection(combatSkills));
        }

       

        Content = content;
        Update();
    }

    private Widget CreateTraitsSection(Pawn pawn)
    {
        var section = CreateSection("Traits");
        var traitsGrid = new Grid
        {
            ColumnSpacing = 16,
            RowSpacing = 12,
            DefaultRowProportion = Proportion.Auto
        };
        traitsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        traitsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        var row = 0;
        foreach (var trait in pawn.Traits)
        {
            // Trait icon indicator
            var indicator = new Panel
            {
                Width = 8,
                Height = 8,
                Background = new SolidBrush(new Color(180, 140, 80)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
            Grid.SetRow(indicator, row);
            Grid.SetColumn(indicator, 0);
            traitsGrid.Widgets.Add(indicator);

            // Trait name and description
            var traitInfo = new VerticalStackPanel { Spacing = 2 };
            traitInfo.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = trait.Label,
                TextColor = new Color(230, 200, 140)
            });
            if (!string.IsNullOrEmpty(trait.Description))
            {
                traitInfo.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = trait.Description,
                    TextColor = new Color(140, 135, 125),
                    Wrap = true
                });
            }
            Grid.SetRow(traitInfo, row);
            Grid.SetColumn(traitInfo, 1);
            traitsGrid.Widgets.Add(traitInfo);

            row++;
        }

        section.Widgets.Add(traitsGrid);
        return section;
    }

    private Widget CreateSkillsSection(List<Skill> skills)
    {
        var section = CreateSection("Combat Skills");

        var skillsGrid = new Grid
        {
            ColumnSpacing = 20,
            RowSpacing = 10,
            DefaultRowProportion = Proportion.Auto
        };
        skillsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Skill name
        skillsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto, 50)); // Level
        skillsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill)); // XP bar

        // Header row
        var headerName = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "SKILL",
            TextColor = new Color(100, 95, 90)
        };
        Grid.SetRow(headerName, 0);
        Grid.SetColumn(headerName, 0);
        skillsGrid.Widgets.Add(headerName);

        var headerLvl = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "LVL",
            TextColor = new Color(100, 95, 90),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetRow(headerLvl, 0);
        Grid.SetColumn(headerLvl, 1);
        skillsGrid.Widgets.Add(headerLvl);

        var headerXp = new Label(BaseContent.Styles.Label.Small)
        {
            Text = "PROGRESS",
            TextColor = new Color(100, 95, 90),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Grid.SetRow(headerXp, 0);
        Grid.SetColumn(headerXp, 2);
        skillsGrid.Widgets.Add(headerXp);

        var row = 1;
        foreach (var skill in skills.OrderBy(s => s.Def.Label))
        {
            var skillRow = new SkillRow(skill, skillsGrid, row++);
            _skillRows[skill] = skillRow;
        }

        section.Widgets.Add(skillsGrid);
        return section;
    }

    

    private static VerticalStackPanel CreateSection(string title)
    {
        var section = new VerticalStackPanel { Spacing = 12 };

        // Section header with line
        var headerRow = new HorizontalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        headerRow.Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = title,
            TextColor = new Color(200, 170, 100)
        });

        var line = new Panel
        {
            Height = 1,
            Background = new SolidBrush(new Color(80, 70, 55)),
            VerticalAlignment = VerticalAlignment.Center
        };
        HorizontalStackPanel.SetProportionType(line, ProportionType.Fill);
        headerRow.Widgets.Add(line);

        section.Widgets.Add(headerRow);
        return section;
    }

    public void Update()
    {
        foreach (var (skill, row) in _skillRows)
        {
            row.Update();
        }
    }

    private sealed class SkillRow
    {
        private readonly Skill _skill;
        private readonly Label _levelLabel;
        private readonly HorizontalProgressBar _xpBar;

        public SkillRow(Skill skill, Grid grid, int gridRow)
        {
            _skill = skill;

            // Skill name
            var nameLabel = new Label(BaseContent.Styles.Label.Normal)
            {
                Text = skill.Def.Label,
                TextColor = new Color(180, 175, 165),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(nameLabel, gridRow);
            Grid.SetColumn(nameLabel, 0);
            grid.Widgets.Add(nameLabel);

            // Level badge
            _levelLabel = new Label(BaseContent.Styles.Label.Medium)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(_levelLabel, gridRow);
            Grid.SetColumn(_levelLabel, 1);
            grid.Widgets.Add(_levelLabel);

            // XP progress bar
            _xpBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Xp)
            {
                Width = 140,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(_xpBar, gridRow);
            Grid.SetColumn(_xpBar, 2);
            grid.Widgets.Add(_xpBar);
        }

        public void Update()
        {
            _levelLabel.Text = _skill.Level.ToString();
            _levelLabel.TextColor = Color.Lerp(new Color(180, 80, 80), new Color(120, 200, 80), _skill.Level / 10f);
            _xpBar.Value = _skill.CurrentLevelXp / _skill.XpRequiredForLevelUp * 100;
        }
    }
}

