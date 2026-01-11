using Grafted.Graphics.Textures;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets;

/// <summary>
/// A persistent horizontal bar that displays earned achievements as clickable icons.
/// Icons remain until manually dismissed by clicking.
/// </summary>
public sealed class AchievementBar : HorizontalStackPanel, IDisposable
{
    private readonly Dictionary<AchievementDef, AchievementBarIcon> _icons = new();
    
    public AchievementBar()
    {
        Spacing = 8;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
        
        // Subscribe to achievement unlocks
        Core.Context.Achievements.AchievementUnlocked += OnAchievementUnlocked;
    }
    
    private void OnAchievementUnlocked(AchievementDef achievement)
    {
        // Don't add duplicate icons
        if (_icons.ContainsKey(achievement)) return;
        
        var icon = new AchievementBarIcon(achievement, OnIconDismissed);
        _icons[achievement] = icon;
        Widgets.Add(icon);
    }
    
    private void OnIconDismissed(AchievementDef achievement)
    {
        if (_icons.TryGetValue(achievement, out var icon))
        {
            Widgets.Remove(icon);
            _icons.Remove(achievement);
        }
    }
    
    public void Dispose()
    {
        Core.Context.Achievements.AchievementUnlocked -= OnAchievementUnlocked;
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
        var container = new VerticalStackPanel { Spacing = 6, MaxWidth = 280 };
        
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
                Text = $"Trait Unlocked: {_achievement.TraitDef.Label}",
                TextColor = new Color(100, 200, 100)
            });
        }
        
        // Unlocked trinket
        if (_achievement.UnlockedTrinketDef != null)
        {
            container.Widgets.Add(new Label(BaseContent.Styles.Label.Small)
            {
                Text = $"Trinket Unlocked: {_achievement.UnlockedTrinketDef.Label}",
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
