namespace Grafted.Scenes.MainGameScene.Gui.Widgets;

public sealed class PlayerAchievementsWindow : Window
{
    private readonly ScrollViewer _scrollViewer;
    private readonly HorizontalStackPanel _categoriesPanel;

    public PlayerAchievementsWindow()
    {
        Title = $"Achievements /c[#b8860b]({GetUnlockedCount()}/{GetTotalCount()} Unlocked)";
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrameBright];
        Padding = new Thickness(12);

        _categoriesPanel = new HorizontalStackPanel { Spacing = 50 };
        _scrollViewer = new ScrollViewer
        {
            Content = _categoriesPanel,
            ShowHorizontalScrollBar = true,
            ShowVerticalScrollBar = true
        };

      

        Content = _scrollViewer;

        RefreshAchievementsList();
    }

    private static int GetUnlockedCount() => Core.Context.Achievements.UnlockedAchievements.Count();
    private static int GetTotalCount() => DefRepository<AchievementDef>.Defs.Count;

    private void RefreshAchievementsList()
    {
        _categoriesPanel.Widgets.Clear();

        var achievementsByCategory = DefRepository<AchievementDef>.Defs
            .GroupBy(a => a.Category)
            .OrderBy(g => g.Key);

        foreach (var category in achievementsByCategory)
        {
            var categoryColumn = new VerticalStackPanel { Spacing = 12 };

            // Category header with count
            var unlockedInCategory = category.Count(a => Core.Context.Achievements.IsUnlocked(a));
            categoryColumn.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
            {
                Text = $"{category.Key} ({unlockedInCategory}/{category.Count()})",
                TextColor = Color.DarkGoldenrod,
                Margin = new Thickness(0, 0, 0, 4)
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
        var hasProgress = !isUnlocked && !isHidden && def.Handler != null && def.TargetValue > 1;

        var container = new VerticalStackPanel
        {
            Spacing = 0
        };

        // Main row with indicator and title
        var mainRow = new HorizontalStackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Status indicator using checkmark icon
        var indicator = new Image
        {
            Background = Stylesheet.Current.Atlas[isUnlocked 
                ? BaseContent.Styles.Atlas.Icon.Checkmark 
                : BaseContent.Styles.Atlas.Icon.X],
            Width = 16,
            Height = 16,
            Opacity = isUnlocked ? 1.0f : 0.3f,
            VerticalAlignment = VerticalAlignment.Center
        };
        mainRow.Widgets.Add(indicator);

        // Title
        var titleColor = isUnlocked ? Color.White : (isHidden ? Color.Gray : new Color(160, 160, 160));
        mainRow.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = isHidden ? "???" : def.Label,
            TextColor = titleColor,
            VerticalAlignment = VerticalAlignment.Center
        });

        container.Widgets.Add(mainRow);

        // Description (smaller, indented to align with title)
        var descText = isHidden ? "Hidden achievement" : def.Description;
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = descText,
            TextColor = isUnlocked ? new Color(140, 140, 140) : new Color(90, 90, 90),
            Margin = new Thickness(24, 0, 0, 0),
            Wrap = true,
            MaxWidth = 300
        });

        // Rewards section for unlocked achievements (combined on one line when possible)
        if (isUnlocked)
        {
            var rewards = new List<string>();
            if (!string.IsNullOrEmpty(def.BenifitDescription))
                rewards.Add(def.BenifitDescription);
            if (def.TraitDef != null)
                rewards.Add($"Trait: {def.TraitDef.Label}");

            if (rewards.Count > 0)
            {
                container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
                {
                    Text = string.Join("\n", rewards),
                    TextColor = new Color(180, 200, 100),
                    Margin = new Thickness(24, 2, 0, 0),
                    Wrap = true,
                    MaxWidth = 300
                });
            }
        }

        // Progress bar using framework's HorizontalProgressBar
        if (hasProgress)
        {
            var current = (int)(progress?.CurrentValue ?? 0);
            var target = (int)def.TargetValue;
            var percent = Math.Min(100f, (float)current / target * 100f);

            var progressContainer = new Panel
            {
                Width = 160,
                Height = 18,
                Margin = new Thickness(24, 2, 0, 0)
            };

            var progressBar = new HorizontalProgressBar(BaseContent.Styles.Bar.Xp)
            {
                Width = 160,
                Height = 18,
                Value = percent
            };
            progressContainer.Widgets.Add(progressBar);

            // Overlay text showing current/target
            progressContainer.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"{current}/{target}",
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            container.Widgets.Add(progressContainer);
        }

        return container;
    }
}
