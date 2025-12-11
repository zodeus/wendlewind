using FontStashSharp;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets;

/// <summary>
/// A single achievement notification that animates in and out
/// </summary>
public class AchievementNotification
{
    public AchievementDef Achievement { get; }
    public float TimeLeft { get; private set; }
    public float TotalDuration { get; }
    public float ElapsedTime { get; private set; }
    
    // Animation states
    public float SlideProgress { get; private set; } // 0 = off screen, 1 = fully visible
    public float GlowIntensity { get; private set; }
    
    public float ScalePulse { get; private set; } = 1f;
    public float SweepProgress { get; private set; } // Light sweep across notification
    public float IconRotation { get; private set; }
    public float TextRevealProgress { get; private set; }
    public float BorderPulse { get; private set; }
    public float RippleProgress { get; private set; }
    
    private const float SlideInDuration = 0.5f;
    private const float SlideOutDuration = 0.6f;
    private const float GlowPulseDuration = 1.2f;
    private const float SweepDuration = 0.8f;
    private const float SweepDelay = 0.3f;
    
    public AchievementNotification(AchievementDef achievement, float duration = 10f)
    {
        Achievement = achievement;
        TimeLeft = duration;
        TotalDuration = duration;
    }
    
    public void Update(float deltaTime)
    {
        ElapsedTime += deltaTime;
        TimeLeft -= deltaTime;
        
        // Slide in animation with overshoot
        if (ElapsedTime < SlideInDuration)
        {
            var t = ElapsedTime / SlideInDuration;
            // Elastic ease out
            SlideProgress = (float)(1 - Math.Pow(2, -10 * t) * Math.Cos(t * Math.PI * 2.5));
            SlideProgress = Math.Clamp(SlideProgress, 0f, 1.05f);
        }
        // Slide out animation
        else if (TimeLeft < SlideOutDuration)
        {
            var t = TimeLeft / SlideOutDuration;
            // Ease in back
            SlideProgress = (float)(t * t * (2.7f * t - 1.7f));
            SlideProgress = Math.Max(0f, SlideProgress);
        }
        else
        {
            SlideProgress = 1f;
        }
        
        // Glow pulse effect - more dramatic
        var glowWave1 = (float)(Math.Sin(ElapsedTime * Math.PI * 2 / GlowPulseDuration) * 0.5 + 0.5);
        var glowWave2 = (float)(Math.Sin(ElapsedTime * Math.PI * 3 / GlowPulseDuration + 0.5) * 0.3 + 0.3);
        GlowIntensity = 0.4f + 0.6f * Math.Max(glowWave1, glowWave2);
        
        // Border pulse
        BorderPulse = 0.7f + 0.3f * (float)Math.Sin(ElapsedTime * Math.PI * 4);
        
        // Scale pulse on entry - more bounce
        if (ElapsedTime < 0.4f)
        {
            var t = ElapsedTime / 0.4f;
            ScalePulse = 1f + 0.2f * (float)(Math.Sin(t * Math.PI) * Math.Pow(1 - t, 0.5));
        }
        else
        {
            ScalePulse = 1f;
        }
        
        // Light sweep effect
        if (ElapsedTime > SweepDelay && ElapsedTime < SweepDelay + SweepDuration)
        {
            var t = (ElapsedTime - SweepDelay) / SweepDuration;
            SweepProgress = (float)Math.Pow(t, 0.5); // Ease out
        }
        else if (ElapsedTime >= SweepDelay + SweepDuration)
        {
            SweepProgress = 1f;
        }
        
        // Icon rotation (slow continuous)
        IconRotation = ElapsedTime * 0.5f;
        
        // Text reveal progress
        TextRevealProgress = Math.Clamp((ElapsedTime - 0.2f) / 0.4f, 0f, 1f);
        
        // Ripple effect on entry
        if (ElapsedTime < 1.5f)
        {
            RippleProgress = ElapsedTime / 1.5f;
        }
        else
        {
            RippleProgress = 1f;
        }
    }
    
    public bool IsExpired => TimeLeft <= 0;
}

/// <summary>
/// Renders achievement unlock notifications with cool animations
/// </summary>
public class AchievementNotificationRenderer : IDisposable
{
    private readonly List<AchievementNotification> _notifications = new();
    private readonly Queue<AchievementDef> _pendingNotifications = new();
    
    private const int MaxVisibleNotifications = 5;
    private const float NotificationSpacing = 12f;
    private const int NotificationWidth = 520;
    private const int NotificationHeight = 90;
    private const float DelayBetweenNotifications = 0.35f;
    
    private float _nextNotificationDelay;
    private float _globalTime;
    
    public AchievementNotificationRenderer()
    {
        // Subscribe to achievement unlocks
        Core.Context.Achievements.AchievementUnlocked += OnAchievementUnlocked;
    }
    
    private void OnAchievementUnlocked(AchievementDef achievement)
    {
        _pendingNotifications.Enqueue(achievement);
    }
    
    public void Update(float deltaTime)
    {
        _globalTime += deltaTime;
        
        // Update existing notifications
        for (var i = _notifications.Count - 1; i >= 0; i--)
        {
            _notifications[i].Update(deltaTime);
            if (_notifications[i].IsExpired)
            {
                _notifications.RemoveAt(i);
            }
        }
        
        // Process pending notifications
        _nextNotificationDelay -= deltaTime;
        if (_pendingNotifications.Count > 0 && 
            _notifications.Count < MaxVisibleNotifications && 
            _nextNotificationDelay <= 0)
        {
            var achievement = _pendingNotifications.Dequeue();
            _notifications.Add(new AchievementNotification(achievement));
            _nextNotificationDelay = DelayBetweenNotifications;
        }
    }
    
    public void Render(SpriteBatch spriteBatch)
    {
        if (_notifications.Count == 0) return;
        
        // Create transform matrix to match UI scaling
        var scale = Core.UiScale;
        var offset = Core.UiOffset;
        var transformMatrix = Matrix.CreateScale(scale, scale, 1f) * 
                              Matrix.CreateTranslation(offset.X, offset.Y, 0);
        
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            transformMatrix
        );
        
        // Use reference resolution for positioning (will be scaled by transform)
        var screenWidth = Core.ReferenceResolution.X;
        var startY = 150f; // Start from top of screen with some margin
        
        for (var i = 0; i < _notifications.Count; i++)
        {
            var notification = _notifications[i];
            var targetY = startY + i * (NotificationHeight + NotificationSpacing);
            
            RenderNotification(spriteBatch, notification, targetY, screenWidth);
        }
        
        spriteBatch.End();
    }
    
    private void RenderNotification(SpriteBatch spriteBatch, AchievementNotification notification, float targetY, int screenWidth)
    {
        var achievement = notification.Achievement;
        
        // Calculate position (slides in from right) - using reference resolution
        var slideOffset = (1f - Math.Min(notification.SlideProgress, 1f)) * (NotificationWidth + 80);
        var x = screenWidth - NotificationWidth - 40 + slideOffset;
        var y = targetY;
        
        var bounds = new Rectangle((int)x, (int)y, NotificationWidth, NotificationHeight);
        
        // Draw glow effect behind the panel
        DrawGlow(spriteBatch, bounds, notification);
        
        // Draw background panel with gradient
        DrawPanel(spriteBatch, bounds, notification);
        
        // Draw light sweep effect
        DrawSweep(spriteBatch, bounds, notification);
        
        // Draw achievement icon area
        var iconBounds = new Rectangle(bounds.X + 12, bounds.Y + 12, 66, 66);
        DrawIconArea(spriteBatch, iconBounds, notification);
        
        // Draw text content
        DrawTextContent(spriteBatch, bounds, achievement, notification);
        
        // Draw decorative elements
        DrawDecorations(spriteBatch, bounds, notification);
        
        // Draw timer bar
        DrawTimerBar(spriteBatch, bounds, notification);
        
        // Draw scanlines for retro effect
        DrawScanlines(spriteBatch, bounds, notification);
    }
    
    private void DrawGlow(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var intensity = notification.GlowIntensity;
        
        // Multi-colored glow layers
        var goldGlow = new Color(255, 180, 50);
        var orangeGlow = new Color(255, 120, 30);
        var whiteGlow = new Color(255, 255, 220);
        
        // Outer orange glow
        for (var i = 5; i >= 0; i--)
        {
            var layerExpand = i * 5 + 8;
            var layerBounds = new Rectangle(
                bounds.X - layerExpand,
                bounds.Y - layerExpand,
                bounds.Width + layerExpand * 2,
                bounds.Height + layerExpand * 2
            );
            var layerAlpha = intensity * (0.08f - i * 0.012f);
            spriteBatch.Draw(pixel, layerBounds, pixel.SourceRect, orangeGlow * layerAlpha);
        }
        
        // Middle gold glow
        for (var i = 3; i >= 0; i--)
        {
            var layerExpand = i * 4 + 4;
            var layerBounds = new Rectangle(
                bounds.X - layerExpand,
                bounds.Y - layerExpand,
                bounds.Width + layerExpand * 2,
                bounds.Height + layerExpand * 2
            );
            var layerAlpha = intensity * (0.12f - i * 0.025f);
            spriteBatch.Draw(pixel, layerBounds, pixel.SourceRect, goldGlow * layerAlpha);
        }
        
        // Inner white hot glow
        var innerExpand = 2;
        var innerBounds = new Rectangle(
            bounds.X - innerExpand,
            bounds.Y - innerExpand,
            bounds.Width + innerExpand * 2,
            bounds.Height + innerExpand * 2
        );
        spriteBatch.Draw(pixel, innerBounds, pixel.SourceRect, whiteGlow * (intensity * 0.15f));
        
        // Ripple effect
        if (notification.RippleProgress < 1f)
        {
            var rippleSize = (int)(notification.RippleProgress * 200);
            var rippleAlpha = (1f - notification.RippleProgress) * 0.3f;
            var rippleBounds = new Rectangle(
                bounds.X + bounds.Width / 2 - rippleSize,
                bounds.Y + bounds.Height / 2 - rippleSize / 2,
                rippleSize * 2,
                rippleSize
            );
            spriteBatch.Draw(pixel, rippleBounds, pixel.SourceRect, goldGlow * rippleAlpha);
        }
    }
    
    private void DrawPanel(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        var borderPulse = notification.BorderPulse;
        
        // Gradient background (darker at top, lighter at bottom)
        var bgColorTop = new Color(15, 12, 8) * alpha;
        var bgColorBottom = new Color(35, 28, 18) * alpha;
        
        // Draw gradient background using horizontal strips
        var stripHeight = 3;
        for (var i = 0; i < bounds.Height; i += stripHeight)
        {
            var t = (float)i / bounds.Height;
            var stripColor = Color.Lerp(bgColorTop, bgColorBottom, t);
            var stripBounds = new Rectangle(bounds.X, bounds.Y + i, bounds.Width, Math.Min(stripHeight, bounds.Height - i));
            spriteBatch.Draw(pixel, stripBounds, pixel.SourceRect, stripColor);
        }
        
        // Outer border (dark)
        var outerBorderColor = new Color(60, 45, 20) * alpha;
        var outerBorderWidth = 4;
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, outerBorderWidth), pixel.SourceRect, outerBorderColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - outerBorderWidth, bounds.Width, outerBorderWidth), pixel.SourceRect, outerBorderColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, outerBorderWidth, bounds.Height), pixel.SourceRect, outerBorderColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - outerBorderWidth, bounds.Y, outerBorderWidth, bounds.Height), pixel.SourceRect, outerBorderColor);
        
        // Inner border (gold, pulsing)
        var innerBorderColor = new Color((int)(200 * borderPulse), (int)(160 * borderPulse), (int)(60 * borderPulse)) * alpha;
        var innerBorderWidth = 2;
        var innerOffset = outerBorderWidth - 1;
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + innerOffset, bounds.Y + innerOffset, bounds.Width - innerOffset * 2, innerBorderWidth), pixel.SourceRect, innerBorderColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + innerOffset, bounds.Bottom - innerOffset - innerBorderWidth, bounds.Width - innerOffset * 2, innerBorderWidth), pixel.SourceRect, innerBorderColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + innerOffset, bounds.Y + innerOffset, innerBorderWidth, bounds.Height - innerOffset * 2), pixel.SourceRect, innerBorderColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - innerOffset - innerBorderWidth, bounds.Y + innerOffset, innerBorderWidth, bounds.Height - innerOffset * 2), pixel.SourceRect, innerBorderColor);
        
        // Top highlight line
        var highlightColor = new Color(255, 220, 120) * (alpha * 0.4f);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + outerBorderWidth + 2, bounds.Y + outerBorderWidth + 2, bounds.Width - (outerBorderWidth + 2) * 2, 1), pixel.SourceRect, highlightColor);
        
        // Bottom shadow line
        var shadowColor = new Color(0, 0, 0) * (alpha * 0.5f);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + outerBorderWidth, bounds.Bottom - outerBorderWidth - 1, bounds.Width - outerBorderWidth * 2, 1), pixel.SourceRect, shadowColor);
    }
    
    private void DrawSweep(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        if (notification.SweepProgress <= 0f || notification.SweepProgress >= 1f) return;
        
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        
        // Diagonal sweep light
        var sweepWidth = 60;
        var sweepX = (int)(bounds.X - sweepWidth + notification.SweepProgress * (bounds.Width + sweepWidth * 2));
        var sweepIntensity = (float)Math.Sin(notification.SweepProgress * Math.PI) * 0.6f;
        
        // Draw sweep as gradient strips
        for (var i = 0; i < sweepWidth; i++)
        {
            var t = (float)i / sweepWidth;
            var stripAlpha = (float)Math.Sin(t * Math.PI) * sweepIntensity * alpha;
            var stripColor = new Color(255, 240, 200) * stripAlpha;
            
            // Diagonal strip
            var x = sweepX + i;
            if (x >= bounds.X && x < bounds.Right)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, bounds.Y + 4, 2, bounds.Height - 8), pixel.SourceRect, stripColor);
            }
        }
    }
    
    private void DrawIconArea(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        
        // Icon background with subtle gradient
        var iconBgTop = new Color(25, 22, 15) * alpha;
        var iconBgBottom = new Color(50, 42, 28) * alpha;
        
        var stripHeight = 2;
        for (var i = 0; i < bounds.Height; i += stripHeight)
        {
            var t = (float)i / bounds.Height;
            var stripColor = Color.Lerp(iconBgTop, iconBgBottom, t);
            var stripBounds = new Rectangle(bounds.X, bounds.Y + i, bounds.Width, Math.Min(stripHeight, bounds.Height - i));
            spriteBatch.Draw(pixel, stripBounds, pixel.SourceRect, stripColor);
        }
        
        // Animated icon border
        var borderPulse = notification.BorderPulse;
        var iconBorderColor = new Color((int)(160 * borderPulse), (int)(130 * borderPulse), (int)(50 * borderPulse)) * alpha;
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), pixel.SourceRect, iconBorderColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), pixel.SourceRect, iconBorderColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), pixel.SourceRect, iconBorderColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), pixel.SourceRect, iconBorderColor);
        
        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        
        // Rotating glow behind icon
        var glowSize = (int)(32 * notification.ScalePulse);
        var glowColor = new Color(255, 200, 80) * (alpha * notification.GlowIntensity * 0.4f);
        
        // Draw rotating beams
        var rotation = notification.IconRotation;
        for (var i = 0; i < 4; i++)
        {
            var angle = rotation + i * Math.PI / 2;
            var beamLength = (int)(glowSize * 0.8f);
            
            // Simplified beam representation
            var beamAlpha = (float)(Math.Sin(angle * 2 + notification.ElapsedTime * 3) * 0.5 + 0.5);
            spriteBatch.Draw(pixel, new Rectangle(centerX - 1, centerY - beamLength / 2, 2, beamLength), pixel.SourceRect, glowColor * beamAlpha);
            spriteBatch.Draw(pixel, new Rectangle(centerX - beamLength / 2, centerY - 1, beamLength, 2), pixel.SourceRect, glowColor * beamAlpha);
        }
        
        // Main icon - stylized cross/plus trophy symbol
        var iconGold = new Color(255, 200, 50) * alpha;
        var iconBright = new Color(255, 240, 150) * alpha;
        var starSize = (int)(22 * notification.ScalePulse);
        
        // Vertical bar
        spriteBatch.Draw(pixel, new Rectangle(centerX - 3, centerY - starSize / 2, 6, starSize), pixel.SourceRect, iconGold);
        // Horizontal bar
        spriteBatch.Draw(pixel, new Rectangle(centerX - starSize / 2, centerY - 3, starSize, 6), pixel.SourceRect, iconGold);
        
        // Bright center
        spriteBatch.Draw(pixel, new Rectangle(centerX - 2, centerY - 2, 4, 4), pixel.SourceRect, iconBright);
        
        // Corner accents on the cross
        var accentSize = (int)(8 * notification.ScalePulse);
        var accentColor = iconGold * 0.8f;
        spriteBatch.Draw(pixel, new Rectangle(centerX - starSize / 2 + 2, centerY - starSize / 2 + 2, accentSize, 2), pixel.SourceRect, accentColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX + starSize / 2 - accentSize - 2, centerY - starSize / 2 + 2, accentSize, 2), pixel.SourceRect, accentColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX - starSize / 2 + 2, centerY + starSize / 2 - 4, accentSize, 2), pixel.SourceRect, accentColor);
        spriteBatch.Draw(pixel, new Rectangle(centerX + starSize / 2 - accentSize - 2, centerY + starSize / 2 - 4, accentSize, 2), pixel.SourceRect, accentColor);
        
        // Sparkle effect on icon
        if (notification.ElapsedTime < 2f)
        {
            var sparklePhase = notification.ElapsedTime * 8;
            var sparkleAlpha = (float)(Math.Sin(sparklePhase) * 0.5 + 0.5) * (1f - notification.ElapsedTime / 2f);
            var sparkleColor = new Color(255, 255, 255) * (alpha * sparkleAlpha);
            var sparkleSize = 3 + (int)(Math.Sin(sparklePhase * 0.7) * 2);
            spriteBatch.Draw(pixel, new Rectangle(centerX - sparkleSize / 2, centerY - sparkleSize / 2, sparkleSize, sparkleSize), pixel.SourceRect, sparkleColor);
        }
    }
    
    private void DrawTextContent(SpriteBatch spriteBatch, Rectangle bounds, AchievementDef achievement, AchievementNotification notification)
    {
        var alpha = Math.Min(notification.SlideProgress, 1f);
        var textReveal = notification.TextRevealProgress;
        var textX = bounds.X + 90; // After icon
        
        // "Achievement Unlocked!" header with typing effect
        var headerFont = BaseContent.Fonts.Default.Small;
        var headerGlow = notification.GlowIntensity;
        var headerColor = new Color(
            (int)(200 + 55 * headerGlow),
            (int)(160 + 40 * headerGlow),
            (int)(80 + 20 * headerGlow)
        ) * alpha;
        var headerText = "ACHIEVEMENT UNLOCKED";
        
        // Reveal text progressively
        var headerRevealLength = (int)(headerText.Length * Math.Min(textReveal * 2, 1f));
        var revealedHeader = headerText[..headerRevealLength];
        spriteBatch.DrawString(headerFont, revealedHeader, new Vector2(textX, bounds.Y + 10), headerColor);
        
        // Achievement name with slight glow
        if (textReveal > 0.3f)
        {
            var nameAlpha = Math.Min((textReveal - 0.3f) / 0.3f, 1f);
            var nameFont = BaseContent.Fonts.Default.Medium;
            
            // Draw glow behind text
            var nameGlowColor = new Color(255, 200, 100) * (alpha * nameAlpha * 0.3f);
            spriteBatch.DrawString(nameFont, achievement.Label, new Vector2(textX + 1, bounds.Y + 29), nameGlowColor);
            spriteBatch.DrawString(nameFont, achievement.Label, new Vector2(textX - 1, bounds.Y + 29), nameGlowColor);
            
            var nameColor = Color.White * (alpha * nameAlpha);
            spriteBatch.DrawString(nameFont, achievement.Label, new Vector2(textX, bounds.Y + 28), nameColor);
        }
        
        // Achievement description
        if (textReveal > 0.6f)
        {
            var descAlpha = Math.Min((textReveal - 0.6f) / 0.4f, 1f);
            var descFont = BaseContent.Fonts.Default.Small;
            var descColor = new Color(160, 155, 140) * (alpha * descAlpha);
            var description = achievement.Description;
            if (description.Length > 50)
            {
                description = description[..47] + "...";
            }
            spriteBatch.DrawString(descFont, description, new Vector2(textX, bounds.Y + 52), descColor);
        }
    }
    
    private void DrawDecorations(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        var time = notification.ElapsedTime;
        
        // Floating particles
        var particleColor = new Color(255, 220, 100);
        for (var i = 0; i < 8; i++)
        {
            var seed = i * 137.5f;
            var particleTime = time + seed;
            var lifePhase = (particleTime * 0.5f) % 3f; // 3 second cycle
            
            if (lifePhase < 2.5f)
            {
                var particleAlpha = lifePhase < 0.5f ? lifePhase * 2 : (lifePhase > 2f ? (2.5f - lifePhase) * 2 : 1f);
                var px = bounds.X + 90 + (int)((seed * 3) % (bounds.Width - 100));
                var py = bounds.Y + bounds.Height - 10 - (int)(lifePhase * 25);
                var drift = (int)(Math.Sin(particleTime * 2 + seed) * 8);
                
                var pSize = 2 + (int)(Math.Sin(seed) + 1);
                var pColor = particleColor * (alpha * particleAlpha * 0.6f * notification.GlowIntensity);
                spriteBatch.Draw(pixel, new Rectangle(px + drift, py, pSize, pSize), pixel.SourceRect, pColor);
            }
        }
        
        // Animated corner brackets
        var cornerOffset = (int)(Math.Sin(time * 3) * 2);
        var accentColor = new Color(220, 180, 80) * (alpha * notification.BorderPulse);
        var accentSize = 12;
        var cornerInset = 6;
        
        // Top-left corner bracket
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + cornerInset + cornerOffset, bounds.Y + cornerInset, accentSize, 2), pixel.SourceRect, accentColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + cornerInset, bounds.Y + cornerInset + cornerOffset, 2, accentSize), pixel.SourceRect, accentColor);
        
        // Top-right corner bracket
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - cornerInset - accentSize - cornerOffset, bounds.Y + cornerInset, accentSize, 2), pixel.SourceRect, accentColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - cornerInset - 2, bounds.Y + cornerInset + cornerOffset, 2, accentSize), pixel.SourceRect, accentColor);
        
        // Bottom-left corner bracket
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + cornerInset + cornerOffset, bounds.Bottom - cornerInset - 2, accentSize, 2), pixel.SourceRect, accentColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + cornerInset, bounds.Bottom - cornerInset - accentSize - cornerOffset, 2, accentSize), pixel.SourceRect, accentColor);
        
        // Bottom-right corner bracket
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - cornerInset - accentSize - cornerOffset, bounds.Bottom - cornerInset - 2, accentSize, 2), pixel.SourceRect, accentColor);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - cornerInset - 2, bounds.Bottom - cornerInset - accentSize - cornerOffset, 2, accentSize), pixel.SourceRect, accentColor);
        
        // Decorative lines
        var lineColor = new Color(180, 140, 50) * (alpha * 0.4f);
        var lineY = bounds.Y + 70;
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + 90, lineY, bounds.Width - 110, 1), pixel.SourceRect, lineColor);
        
        // Small diamond accents along the line
        for (var i = 0; i < 3; i++)
        {
            var diamondX = bounds.X + 100 + i * 60;
            var diamondPulse = (float)(Math.Sin(time * 4 + i * 0.5) * 0.5 + 0.5);
            var diamondColor = new Color(255, 200, 80) * (alpha * diamondPulse * 0.6f);
            spriteBatch.Draw(pixel, new Rectangle(diamondX, lineY - 1, 3, 3), pixel.SourceRect, diamondColor);
        }
    }
    
    private void DrawTimerBar(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        
        // Timer bar at the bottom
        var barHeight = 3;
        var barY = bounds.Bottom - 6;
        var barPadding = 8;
        var maxBarWidth = bounds.Width - barPadding * 2;
        
        // Background bar
        var bgColor = new Color(40, 35, 25) * alpha;
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + barPadding, barY, maxBarWidth, barHeight), pixel.SourceRect, bgColor);
        
        // Progress bar
        var progress = notification.TimeLeft / notification.TotalDuration;
        var barWidth = (int)(maxBarWidth * progress);
        
        // Color shifts from gold to orange to red as time runs out
        Color barColor;
        if (progress > 0.5f)
        {
            barColor = new Color(255, 200, 50); // Gold
        }
        else if (progress > 0.2f)
        {
            var t = (progress - 0.2f) / 0.3f;
            barColor = Color.Lerp(new Color(255, 120, 30), new Color(255, 200, 50), t); // Orange to gold
        }
        else
        {
            var t = progress / 0.2f;
            barColor = Color.Lerp(new Color(200, 60, 40), new Color(255, 120, 30), t); // Red to orange
        }
        
        barColor *= alpha;
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + barPadding, barY, barWidth, barHeight), pixel.SourceRect, barColor);
        
        // Bright edge
        if (barWidth > 2)
        {
            var edgeColor = new Color(255, 255, 200) * (alpha * 0.6f);
            spriteBatch.Draw(pixel, new Rectangle(bounds.X + barPadding + barWidth - 2, barY, 2, barHeight), pixel.SourceRect, edgeColor);
        }
    }
    
    private void DrawScanlines(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f) * 0.08f; // Very subtle
        var scanlineColor = new Color(0, 0, 0) * alpha;
        
        // Draw horizontal scanlines every 3 pixels
        for (var y = bounds.Y + 4; y < bounds.Bottom - 4; y += 3)
        {
            spriteBatch.Draw(pixel, new Rectangle(bounds.X + 4, y, bounds.Width - 8, 1), pixel.SourceRect, scanlineColor);
        }
    }
    
    public void Dispose()
    {
        Core.Context.Achievements.AchievementUnlocked -= OnAchievementUnlocked;
    }
    
    /// <summary>
    /// Manually trigger a notification (for testing)
    /// </summary>
    public void ShowNotification(AchievementDef achievement)
    {
        _pendingNotifications.Enqueue(achievement);
    }
}

