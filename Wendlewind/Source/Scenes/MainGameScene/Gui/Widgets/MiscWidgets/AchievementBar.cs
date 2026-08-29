using Wendlewind.Graphics.Textures;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

/// <summary>
/// A persistent horizontal bar that displays earned achievements as clickable icons.
/// Icons remain until manually dismissed by clicking.
/// When more than 8 achievements exist, shows 7 icons plus a "more" indicator.
/// </summary>
public sealed class AchievementBar : HorizontalStackPanel, IDisposable
{
    private const int MaxVisibleIcons = 8;

    private readonly Dictionary<AchievementDef, AchievementBarIcon> _visibleIcons = new();
    private readonly List<AchievementDef> _allAchievements = new();
    private MoreIndicator? _moreIndicator;

    public AchievementBar()
    {
        Spacing = 8;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;

        // Add icons for any unacknowledged achievements (persists across screen transitions)
        foreach (var progress in Core.Context.Achievements.UnacknowledgedAchievements)
        {
            AddAchievement(progress.Def);
        }

        // Subscribe to new achievement unlocks
        Core.Context.Achievements.AchievementUnlocked += OnAchievementUnlocked;
    }

    private void OnAchievementUnlocked(AchievementDef achievement)
    {
        AddAchievement(achievement);
    }

    private void AddAchievement(AchievementDef achievement)
    {
        // Don't add duplicates
        if (_allAchievements.Contains(achievement)) return;

        _allAchievements.Add(achievement);
        RebuildDisplay();
    }

    private void RebuildDisplay()
    {
        // Clear current display
        Widgets.Clear();
        _visibleIcons.Clear();
        _moreIndicator = null;

        if (_allAchievements.Count <= MaxVisibleIcons)
        {
            // Show all achievements
            foreach (var achievement in _allAchievements)
            {
                var icon = new AchievementBarIcon(achievement, OnIconDismissed);
                _visibleIcons[achievement] = icon;
                Widgets.Add(icon);
            }
        }
        else
        {
            // Show first 7 achievements + more indicator
            var visibleCount = MaxVisibleIcons - 1;
            for (var i = 0; i < visibleCount; i++)
            {
                var achievement = _allAchievements[i];
                var icon = new AchievementBarIcon(achievement, OnIconDismissed);
                _visibleIcons[achievement] = icon;
                Widgets.Add(icon);
            }

            // Add the "more" indicator
            var hiddenAchievements = _allAchievements.Skip(visibleCount).ToList();
            _moreIndicator = new MoreIndicator(hiddenAchievements, OnIconDismissed);
            Widgets.Add(_moreIndicator);
        }
    }

    private void OnIconDismissed(AchievementDef achievement)
    {
        if (_allAchievements.Remove(achievement))
        {
            // Mark as acknowledged in the tracker so it persists
            Core.Context.Achievements.Acknowledge(achievement);
            RebuildDisplay();
        }
    }

    public void Dispose()
    {
        Core.Context.Achievements.AchievementUnlocked -= OnAchievementUnlocked;
    }
}

/// <summary>
/// A "more" indicator that shows the count of additional achievements and provides
/// a tooltip with all hidden achievements that can be individually dismissed.
/// </summary>
internal sealed class MoreIndicator : Panel
{
    private readonly List<AchievementDef> _hiddenAchievements;
    private readonly Action<AchievementDef> _onDismiss;
    private readonly CursorButton _button;

    private const int IconSize = 56;

    public MoreIndicator(List<AchievementDef> hiddenAchievements, Action<AchievementDef> onDismiss)
    {
        _hiddenAchievements = hiddenAchievements;
        _onDismiss = onDismiss;

        _button = new CursorButton(BaseContent.Styles.Button.GreenGold)
        {
            Width = IconSize,
            Height = IconSize,
            Content = new Label(BaseContent.Styles.Label.Normal)
            {
                Text = $"+{_hiddenAchievements.Count}",
                TextColor = Color.Black,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        _button.WithTooltip(() => CreateTooltipContent());
        _button.MouseEntered += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
        _button.MouseLeft += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);

        Widgets.Add(_button);
    }

    private Widget CreateTooltipContent()
    {
        var container = new VerticalStackPanel { Spacing = 8, MaxWidth = 400 };

        container.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = $"{_hiddenAchievements.Count} More Achievements",
            TextColor = Color.Gold
        });

        container.Widgets.Add(new HorizontalSeparator { Color = new Color(60, 50, 40) });

        foreach (var achievement in _hiddenAchievements)
        {
            var row = new HorizontalStackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

            // Achievement name
            row.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"• {achievement.Label}",
                TextColor = new Color(200, 200, 200)
            });

            container.Widgets.Add(row);
        }

        container.Widgets.Add(new HorizontalSeparator { Color = new Color(40, 40, 40) });
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Click individual achievements to dismiss",
            TextColor = new Color(120, 120, 120)
        });

        return container;
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        TooltipHelper.UpdatePosition();
    }
}

/// <summary>
/// A single achievement icon in the bar with tooltip and click-to-dismiss.
/// </summary>
internal sealed class AchievementBarIcon : Panel
{
    private readonly AchievementDef _achievement;
    private readonly Action<AchievementDef> _onDismiss;
    private readonly CursorButton _button;
    private Texture2D? _iconTexture;

    private const int IconSize = 56;

    public AchievementBarIcon(AchievementDef achievement, Action<AchievementDef> onDismiss)
    {
        _achievement = achievement;
        _onDismiss = onDismiss;

        var iconRegion = GetIconRegion();

        _button = new CursorButton(BaseContent.Styles.Button.GreenGold)
        {
            Width = IconSize,
            Height = IconSize,
            Content = new Image
            {
                Background = iconRegion,
                Width = IconSize - 16,
                Height = IconSize - 16,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        _button.Click += OnClick;
        _button.WithTooltip(() => CreateTooltipContent());
        _button.MouseEntered += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Hand);
        _button.MouseLeft += (_, _) => Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);

        Widgets.Add(_button);
    }

    private IImage GetIconRegion()
    {
        // Try to load custom icon from IconPath, fall back to default achievement icon
        if (!string.IsNullOrEmpty(_achievement.IconPath))
        {
            try
            {
                _iconTexture = Core.Content.Load<Texture2D>(_achievement.IconPath);
                if (_iconTexture != null)
                {
                    return new TextureRegion(TextureUtils.PreMultiply(_iconTexture)!);
                }
            }
            catch
            {
                // Fall through to default
            }
        }

        // Use default achievements icon from atlas
        return Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Icon.Achievements];
    }

    private Widget CreateTooltipContent()
    {
        var container = new VerticalStackPanel { Spacing = 6 };

        // Title with golden color
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Normal)
        {
            Text = _achievement.Label,
            TextColor = Color.Gold
        });

        // Description
        if (!string.IsNullOrEmpty(_achievement.Description))
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = _achievement.Description,
                TextColor = new Color(180, 180, 180),
                Wrap = true
            });
        }

        // Benefit description
        if (!string.IsNullOrEmpty(_achievement.BenifitDescription))
        {
            container.Widgets.Add(new HorizontalSeparator { Color = new Color(60, 50, 40) });
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = _achievement.BenifitDescription,
                TextColor = new Color(189, 157, 42)
            });
        }

        // Unlocked trait
        if (_achievement.TraitDef != null)
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Trait: {_achievement.TraitDef.Label}",
                TextColor = new Color(100, 200, 100)
            });
        }

        // Unlocked trinket
        if (_achievement.UnlockedTrinketDef != null)
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Trinket: {_achievement.UnlockedTrinketDef.Label}",
                TextColor = new Color(100, 180, 220)
            });
        }

        // Dismiss hint
        container.Widgets.Add(new HorizontalSeparator { Color = new Color(40, 40, 40) });
        container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
        {
            Text = "Click to dismiss",
            TextColor = new Color(120, 120, 120)
        });

        return container;
    }

    private void OnClick(object? sender, EventArgs e)
    {
        _onDismiss(_achievement);
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        TooltipHelper.UpdatePosition();
    }
}
