using Myra.Graphics2D.Brushes;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets;

public sealed class PlayerAchievementsWindow : Window
{
    private readonly ScrollViewer _scrollViewer;
    private readonly HorizontalStackPanel _categoriesPanel;

    public PlayerAchievementsWindow()
    {
        Title = "Achievements";
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
        Padding = new Thickness(20);

        _categoriesPanel = new HorizontalStackPanel { Spacing = 20 };
        _scrollViewer = new ScrollViewer
        {
            Content = _categoriesPanel,
            ShowHorizontalScrollBar = true,
            ShowVerticalScrollBar = true
        };

        var header = new HorizontalStackPanel
        {
            Spacing = 20,
            Margin = new Thickness(0, 0, 0, 15),
            Widgets =
            {
                CreateStatLabel("Unlocked", GetUnlockedCount(), GetTotalCount()),
            }
        };

        Content = new VerticalStackPanel
        {
            Widgets =
            {
                header,
                new HorizontalSeparator { Margin = new Thickness(0, 0, 0, 10) },
                _scrollViewer
            }
        };

        RefreshAchievementsList();
    }

    private static int GetUnlockedCount() => Core.Context.Achievements.UnlockedAchievements.Count();
    private static int GetTotalCount() => DefRepository<AchievementDef>.Defs.Count;

    private static Label CreateStatLabel(string label, int value, int? total = null)
    {
        var text = total.HasValue ? $"{label}: {value}/{total}" : $"{label}: {value}";
        return new Label(BaseContent.Styles.Label.Normal)
        {
            Text = text,
            TextColor = Color.LightGray
        };
    }

    private void RefreshAchievementsList()
    {
        _categoriesPanel.Widgets.Clear();

        var achievementsByCategory = DefRepository<AchievementDef>.Defs
            .GroupBy(a => a.Category)
            .OrderBy(g => g.Key);

        foreach (var category in achievementsByCategory)
        {
            var categoryColumn = new VerticalStackPanel { Spacing = 8, MaxWidth = 400 };

            // Category header
            categoryColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = category.Key,
                TextColor = Color.Gold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            foreach (var def in category.OrderByDescending(a => Core.Context.Achievements.IsUnlocked(a)))
            {
                categoryColumn.Widgets.Add(CreateAchievementRow(def));
            }

            _categoriesPanel.Widgets.Add(categoryColumn);
        }
    }

    private Widget CreateAchievementRow(AchievementDef def)
    {
        var progress = Core.Context.Achievements.GetProgress(def);
        var isUnlocked = progress?.IsUnlocked ?? false;
        var isHidden = def.IsHidden && !isUnlocked;

        var panel = new HorizontalStackPanel
        {
            Spacing = 10,
            Margin = new Thickness(10, 2, 0, 2),
            VerticalAlignment = VerticalAlignment.Center
        };

        // Status icon
        var statusIcon = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = isUnlocked ? "[X]" : "[ ]",
            TextColor = isUnlocked ? Color.LimeGreen : Color.Gray,
            Width = 40
        };
        panel.Widgets.Add(statusIcon);

        // Achievement info
        var infoPanel = new VerticalStackPanel { Spacing = 2 };

        var title = new Label(BaseContent.Styles.Label.Normal)
        {
            Text = isHidden ? "???" : def.Label,
            TextColor = isUnlocked ? Color.White : Color.DarkGray
        };
        infoPanel.Widgets.Add(title);

        var description = new Label(BaseContent.Styles.Label.Small)
        {
            Text = isHidden ? "Hidden achievement" : def.Description,
            TextColor = isUnlocked ? Color.LightGray : Color.DimGray,
            Wrap = true, MaxWidth = 300
        }; 
        infoPanel.Widgets.Add(description);

        // Progress bar for achievements with handlers and target values > 1
        if (!isUnlocked && !isHidden && def.Handler != null && def.TargetValue >= 1)
        {
            var progressBar = CreateProgressBar((int)(progress?.CurrentValue ?? 0), (int)def.TargetValue);
            infoPanel.Widgets.Add(progressBar);
        }

        if (isUnlocked && def.BenifitDescription != "")
        {
            infoPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = def.BenifitDescription,
                TextColor = Color.Gold,
                Wrap = true,
                MaxWidth = 250
            });
        }

        if (isUnlocked && def.TraitDef != null)
        {
            infoPanel.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Trait: {def.TraitDef.Label}",
                TextColor = Color.GreenYellow,
                Wrap = true,
                MaxWidth = 250
            });
        }

        panel.Widgets.Add(infoPanel);

        return panel;
    }

    private static Widget CreateProgressBar(int current, int target)
    {
        var percent = Math.Min(1f, (float)current / target);
        var barWidth = 200;

        var container = new Panel
        {
            Width = barWidth,
            Margin = new Thickness(0, 4, 0, 0)
        };

        // Background
        var background = new Panel
        {
            Width = barWidth,
            Height = 24,
            Background = new SolidBrush(new Color(40, 40, 40))
        };
        container.Widgets.Add(background);

        // Fill
        var fill = new Panel
        {
            Width = (int)(barWidth * percent),
            Height = 24,
            Background = new SolidBrush(new Color(80, 120, 80))
        };
        container.Widgets.Add(fill);

        // Text
        var text = new Label(BaseContent.Styles.Label.Small)
        {
            Text = $"{current}/{target}",
            TextColor = Color.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        container.Widgets.Add(text);

        return container;
    }
}
