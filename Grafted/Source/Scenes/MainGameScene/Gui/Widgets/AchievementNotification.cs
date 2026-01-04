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
    public float StarBurstProgress { get; private set; } // Initial star burst effect
    public float ShineProgress { get; private set; } // Border shine traveling around
    
    private const float SlideInDuration = 0.4f;
    private const float SlideOutDuration = 0.5f;
    private const float GlowPulseDuration = 2.0f;
    private const float SweepDuration = 0.6f;
    private const float SweepDelay = 0.2f;
    
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
        
        // Slide in animation with smooth ease out
        if (ElapsedTime < SlideInDuration)
        {
            var t = ElapsedTime / SlideInDuration;
            // Smooth elastic ease out with less bounce
            SlideProgress = (float)(1 - Math.Pow(2, -8 * t) * Math.Cos(t * Math.PI * 1.8));
            SlideProgress = Math.Clamp(SlideProgress, 0f, 1.02f);
        }
        // Slide out animation
        else if (TimeLeft < SlideOutDuration)
        {
            var t = TimeLeft / SlideOutDuration;
            // Smooth ease in
            SlideProgress = t * t * t;
            SlideProgress = Math.Max(0f, SlideProgress);
        }
        else
        {
            SlideProgress = 1f;
        }
        
        // Glow pulse effect - smooth breathing
        var glowWave = (float)(Math.Sin(ElapsedTime * Math.PI / GlowPulseDuration) * 0.5 + 0.5);
        GlowIntensity = 0.5f + 0.5f * glowWave;
        
        // Border pulse - gentler
        BorderPulse = 0.8f + 0.2f * (float)Math.Sin(ElapsedTime * Math.PI * 2);
        
        // Scale pulse on entry - subtle
        if (ElapsedTime < 0.3f)
        {
            var t = ElapsedTime / 0.3f;
            ScalePulse = 1f + 0.08f * (float)(Math.Sin(t * Math.PI));
        }
        else
        {
            ScalePulse = 1f;
        }
        
        // Light sweep effect - faster, more dramatic
        if (ElapsedTime > SweepDelay && ElapsedTime < SweepDelay + SweepDuration)
        {
            var t = (ElapsedTime - SweepDelay) / SweepDuration;
            SweepProgress = (float)Math.Pow(t, 0.4); // Faster ease out
        }
        else if (ElapsedTime >= SweepDelay + SweepDuration)
        {
            SweepProgress = 1f;
        }
        
        // Star burst on entry
        if (ElapsedTime < 0.8f)
        {
            StarBurstProgress = ElapsedTime / 0.8f;
        }
        else
        {
            StarBurstProgress = 1f;
        }
        
        // Border shine - continuous traveling effect
        ShineProgress = (ElapsedTime * 0.4f) % 1f;
        
        // Icon rotation (very slow)
        IconRotation = ElapsedTime * 0.3f;
        
        // Text reveal progress - faster
        TextRevealProgress = Math.Clamp((ElapsedTime - 0.1f) / 0.3f, 0f, 1f);
        
        // Ripple effect on entry
        if (ElapsedTime < 1.0f)
        {
            RippleProgress = ElapsedTime / 1.0f;
        }
        else
        {
            RippleProgress = 1f;
        }
    }
    
    public bool IsExpired => TimeLeft <= 0;
}

/// <summary>
/// Renders achievement unlock notifications with polished animations
/// </summary>
public class AchievementNotificationRenderer : IDisposable
{
    private readonly List<AchievementNotification> _notifications = new();
    private readonly Queue<AchievementDef> _pendingNotifications = new();
    
    private const int MaxVisibleNotifications = 5;
    private const float NotificationSpacing = 16f;
    private const int NotificationWidth = 380;
    private const int NotificationHeight = 100;
    private const float DelayBetweenNotifications = 0.4f;
    
    // Corner chamfer size for the beveled look
    private const int CornerSize = 10;
    
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
        var startY = 120f;
        
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
        
        // Calculate position (slides in from right)
        var slideOffset = (1f - Math.Min(notification.SlideProgress, 1f)) * (NotificationWidth + 60);
        var x = screenWidth - NotificationWidth - 24 + slideOffset;
        var y = targetY;
        
        var bounds = new Rectangle((int)x, (int)y, NotificationWidth, NotificationHeight);
        
        // Draw star burst effect on entry
        DrawStarBurst(spriteBatch, bounds, notification);
        
        // Draw outer glow
        DrawGlow(spriteBatch, bounds, notification);
        
        // Draw main panel with chamfered corners
        DrawPanel(spriteBatch, bounds, notification);
        
        // Draw traveling border shine
        DrawBorderShine(spriteBatch, bounds, notification);
        
        // Draw light sweep effect
        DrawSweep(spriteBatch, bounds, notification);
        
        // Draw text content
        DrawTextContent(spriteBatch, bounds, achievement, notification);
        
        // Draw floating particles
        DrawParticles(spriteBatch, bounds, notification);
        
        // Draw timer bar
        DrawTimerBar(spriteBatch, bounds, notification);
    }
    
    private void DrawStarBurst(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        if (notification.StarBurstProgress >= 1f) return;

        var pixel = Core.Graphics.PixelTexture;
        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        
        var progress = notification.StarBurstProgress;
        var fadeOut = 1f - progress;
        
        // Draw expanding rays
        var rayCount = 12;
        for (var i = 0; i < rayCount; i++)
        {
            var angle = (i / (float)rayCount) * Math.PI * 2;
            var rayLength = (int)(progress * 120);
            var rayWidth = (int)(3 * fadeOut) + 1;
            
            var endX = centerX + (int)(Math.Cos(angle) * rayLength);
            var endY = centerY + (int)(Math.Sin(angle) * rayLength);
            
            // Draw ray as a line approximation
            var rayColor = new Color(255, 240, 180) * (fadeOut * 0.6f);
            
            // Simple ray drawing (horizontal/vertical for efficiency)
            if (Math.Abs(Math.Cos(angle)) > Math.Abs(Math.Sin(angle)))
            {
                var minX = Math.Min(centerX, endX);
                var maxX = Math.Max(centerX, endX);
                spriteBatch.Draw(pixel, new Rectangle(minX, centerY - rayWidth / 2, maxX - minX, rayWidth), pixel.SourceRect, rayColor);
            }
            else
            {
                var minY = Math.Min(centerY, endY);
                var maxY = Math.Max(centerY, endY);
                spriteBatch.Draw(pixel, new Rectangle(centerX - rayWidth / 2, minY, rayWidth, maxY - minY), pixel.SourceRect, rayColor);
            }
        }
    }
    
    private void DrawGlow(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var intensity = notification.GlowIntensity;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        
        // Rich warm glow colors
        var innerGlow = new Color(255, 200, 80);
        var outerGlow = new Color(200, 120, 40);
        
        // Soft outer glow
        for (var i = 4; i >= 0; i--)
        {
            var expand = i * 6 + 4;
            var glowBounds = new Rectangle(
                bounds.X - expand,
                bounds.Y - expand,
                bounds.Width + expand * 2,
                bounds.Height + expand * 2
            );
            var glowAlpha = intensity * alpha * (0.06f - i * 0.01f);
            spriteBatch.Draw(pixel, glowBounds, pixel.SourceRect, outerGlow * glowAlpha);
        }
        
        // Tight inner glow
        for (var i = 2; i >= 0; i--)
        {
            var expand = i * 2 + 1;
            var glowBounds = new Rectangle(
                bounds.X - expand,
                bounds.Y - expand,
                bounds.Width + expand * 2,
                bounds.Height + expand * 2
            );
            var glowAlpha = intensity * alpha * (0.15f - i * 0.04f);
            spriteBatch.Draw(pixel, glowBounds, pixel.SourceRect, innerGlow * glowAlpha);
        }
    }
    
    private void DrawPanel(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        var borderPulse = notification.BorderPulse;
        
        // Rich dark background colors
        var bgDark = new Color(12, 10, 8) * alpha;
        var bgMid = new Color(22, 18, 14) * alpha;
        var bgLight = new Color(32, 26, 20) * alpha;
        
        // Main background with vertical gradient
        for (var y = 0; y < bounds.Height; y++)
        {
            float t = y / (float)bounds.Height;
            Color rowColor;
            if (t < 0.3f)
                rowColor = Color.Lerp(bgDark, bgMid, t / 0.3f);
            else if (t < 0.7f)
                rowColor = bgMid;
            else
                rowColor = Color.Lerp(bgMid, bgLight, (t - 0.7f) / 0.3f);
            
            // Skip corners for chamfer
            var xStart = bounds.X;
            var xEnd = bounds.Right;
            var yPos = bounds.Y + y;
            
            // Top corners
            if (y < CornerSize)
            {
                xStart = bounds.X + (CornerSize - y);
                xEnd = bounds.Right - (CornerSize - y);
            }
            // Bottom corners
            else if (y >= bounds.Height - CornerSize)
            {
                var fromBottom = bounds.Height - y - 1;
                xStart = bounds.X + (CornerSize - fromBottom);
                xEnd = bounds.Right - (CornerSize - fromBottom);
            }
            
            if (xEnd > xStart)
            {
                spriteBatch.Draw(pixel, new Rectangle(xStart, yPos, xEnd - xStart, 1), pixel.SourceRect, rowColor);
            }
        }
        
        // Outer frame - dark border
        var frameDark = new Color(45, 36, 24) * alpha;
        var frameGold = new Color((int)(180 * borderPulse), (int)(140 * borderPulse), (int)(60 * borderPulse)) * alpha;
        var frameBright = new Color((int)(220 * borderPulse), (int)(180 * borderPulse), (int)(80 * borderPulse)) * alpha;
        
        // Draw chamfered border outline
        DrawChamferedBorder(spriteBatch, bounds, frameDark, 3, CornerSize);
        DrawChamferedBorder(spriteBatch, bounds, frameGold, 2, CornerSize);
        
        // Inner highlight line along top
        var highlightColor = new Color(255, 220, 140) * (alpha * 0.3f);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + CornerSize + 4, bounds.Y + 4, bounds.Width - (CornerSize + 4) * 2, 1), pixel.SourceRect, highlightColor);
        
        // Subtle inner shadow at bottom
        var shadowColor = new Color(0, 0, 0) * (alpha * 0.4f);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + CornerSize + 4, bounds.Bottom - 5, bounds.Width - (CornerSize + 4) * 2, 1), pixel.SourceRect, shadowColor);
    }
    
    private void DrawChamferedBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int thickness, int chamfer)
    {
        var pixel = Core.Graphics.PixelTexture;
        
        // Top edge (between chamfers)
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + chamfer, bounds.Y, bounds.Width - chamfer * 2, thickness), pixel.SourceRect, color);
        
        // Bottom edge
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + chamfer, bounds.Bottom - thickness, bounds.Width - chamfer * 2, thickness), pixel.SourceRect, color);
        
        // Left edge
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y + chamfer, thickness, bounds.Height - chamfer * 2), pixel.SourceRect, color);
        
        // Right edge
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - thickness, bounds.Y + chamfer, thickness, bounds.Height - chamfer * 2), pixel.SourceRect, color);
        
        // Chamfer corners (diagonal lines approximated)
        for (var i = 0; i < chamfer; i++)
        {
            // Top-left
            spriteBatch.Draw(pixel, new Rectangle(bounds.X + i, bounds.Y + chamfer - i - 1, thickness, thickness), pixel.SourceRect, color);
            // Top-right
            spriteBatch.Draw(pixel, new Rectangle(bounds.Right - i - thickness, bounds.Y + chamfer - i - 1, thickness, thickness), pixel.SourceRect, color);
            // Bottom-left
            spriteBatch.Draw(pixel, new Rectangle(bounds.X + i, bounds.Bottom - chamfer + i, thickness, thickness), pixel.SourceRect, color);
            // Bottom-right
            spriteBatch.Draw(pixel, new Rectangle(bounds.Right - i - thickness, bounds.Bottom - chamfer + i, thickness, thickness), pixel.SourceRect, color);
        }
    }
    
    private void DrawBorderShine(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        var shinePos = notification.ShineProgress;
        
        // Calculate total perimeter for shine position
        var topWidth = bounds.Width - CornerSize * 2;
        var sideHeight = bounds.Height - CornerSize * 2;
        var totalPerimeter = (topWidth + sideHeight) * 2 + CornerSize * 4 * 1.414f;
        
        var shineLength = 40;
        var shineColor = new Color(255, 255, 220) * (alpha * 0.7f);
        
        // Calculate shine position along border
        var pos = shinePos * totalPerimeter;
        
        // Draw shine segment on top edge for simplicity
        if (pos < topWidth)
        {
            var shineX = bounds.X + CornerSize + (int)pos;
            var actualLength = Math.Min(shineLength, bounds.Right - CornerSize - shineX);
            if (actualLength > 0)
            {
                for (var i = 0; i < actualLength; i++)
                {
                    var intensity = 1f - Math.Abs(i - actualLength / 2f) / (actualLength / 2f);
                    spriteBatch.Draw(pixel, new Rectangle(shineX + i, bounds.Y + 1, 1, 2), pixel.SourceRect, shineColor * intensity);
                }
            }
        }
    }
    
    private void DrawSweep(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        if (notification.SweepProgress <= 0f || notification.SweepProgress >= 1f) return;
        
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        
        // Wide diagonal sweep
        var sweepWidth = 80;
        var sweepX = (int)(bounds.X - sweepWidth + notification.SweepProgress * (bounds.Width + sweepWidth * 2));
        var sweepIntensity = (float)Math.Sin(notification.SweepProgress * Math.PI) * 0.4f;
        
        for (var i = 0; i < sweepWidth; i++)
        {
            var t = (float)i / sweepWidth;
            var stripAlpha = (float)Math.Sin(t * Math.PI) * sweepIntensity * alpha;
            var stripColor = new Color(255, 250, 230) * stripAlpha;
            
            var x = sweepX + i;
            if (x >= bounds.X + CornerSize && x < bounds.Right - CornerSize)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, bounds.Y + CornerSize, 1, bounds.Height - CornerSize * 2), pixel.SourceRect, stripColor);
            }
        }
    }
    
    private void DrawTextContent(SpriteBatch spriteBatch, Rectangle bounds, AchievementDef achievement, AchievementNotification notification)
    {
        var alpha = Math.Min(notification.SlideProgress, 1f);
        var textReveal = notification.TextRevealProgress;
        var textX = bounds.X + CornerSize + 8;
        
        // Header - "ACHIEVEMENT UNLOCKED"
        var headerFont = BaseContent.Fonts.Default.Small;
        var headerGlow = notification.GlowIntensity;
        var headerColor = new Color(
            (int)(220 + 35 * headerGlow),
            (int)(180 + 30 * headerGlow),
            (int)(100 + 20 * headerGlow)
        ) * alpha;
        
        var headerText = "ACHIEVEMENT UNLOCKED";
        var headerRevealLen = (int)(headerText.Length * Math.Min(textReveal * 1.5f, 1f));
        var revealedHeader = headerText[..headerRevealLen];
        
        // Header shadow
        var headerShadow = new Color(0, 0, 0) * (alpha * 0.5f);
        spriteBatch.DrawString(headerFont, revealedHeader, new Vector2(textX + 1, bounds.Y + 15), headerShadow);
        spriteBatch.DrawString(headerFont, revealedHeader, new Vector2(textX, bounds.Y + 14), headerColor);
        
        // Achievement name
        if (textReveal > 0.2f)
        {
            var nameAlpha = Math.Min((textReveal - 0.2f) / 0.3f, 1f);
            var nameFont = BaseContent.Fonts.Default.Medium;
            
            // Name glow
            var nameGlowColor = new Color(255, 220, 120) * (alpha * nameAlpha * 0.25f);
            spriteBatch.DrawString(nameFont, achievement.Label, new Vector2(textX + 1, bounds.Y + 37), nameGlowColor);
            spriteBatch.DrawString(nameFont, achievement.Label, new Vector2(textX - 1, bounds.Y + 37), nameGlowColor);
            spriteBatch.DrawString(nameFont, achievement.Label, new Vector2(textX, bounds.Y + 38), nameGlowColor);
            
            // Name shadow and main
            var nameShadow = new Color(0, 0, 0) * (alpha * nameAlpha * 0.6f);
            spriteBatch.DrawString(nameFont, achievement.Label, new Vector2(textX + 1, bounds.Y + 37), nameShadow);
            
            var nameColor = Color.White * (alpha * nameAlpha);
            spriteBatch.DrawString(nameFont, achievement.Label, new Vector2(textX, bounds.Y + 36), nameColor);
        }
        
        // Description
        if (textReveal > 0.5f)
        {
            var descAlpha = Math.Min((textReveal - 0.5f) / 0.4f, 1f);
            var descFont = BaseContent.Fonts.Default.Small;
            var descColor = new Color(170, 165, 150) * (alpha * descAlpha);
            
            var description = achievement.Description;
            if (description.Length > 50)
            {
                description = description[..47] + "...";
            }
            
            // Description shadow
            var descShadow = new Color(0, 0, 0) * (alpha * descAlpha * 0.4f);
            spriteBatch.DrawString(descFont, description, new Vector2(textX + 1, bounds.Y + 63), descShadow);
            spriteBatch.DrawString(descFont, description, new Vector2(textX, bounds.Y + 62), descColor);
        }
    }
    
    private void DrawParticles(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        var time = notification.ElapsedTime;
        
        // Sparkle particles rising across the notification
        var particleColors = new[]
        {
            new Color(255, 240, 180),
            new Color(255, 220, 120),
            new Color(255, 200, 80)
        };
        
        for (var i = 0; i < 8; i++)
        {
            var seed = i * 97.3f;
            var particleTime = (time * 0.7f + seed * 0.01f) % 2.5f;
            
            if (particleTime < 2f)
            {
                var fadeIn = Math.Min(particleTime * 4, 1f);
                var fadeOut = particleTime > 1.5f ? (2f - particleTime) * 2 : 1f;
                var particleAlpha = fadeIn * fadeOut;
                
                // Spread across the notification width
                var startX = bounds.X + 20 + (int)((seed * 2.3f) % (bounds.Width - 40));
                var startY = bounds.Y + bounds.Height - 15;
                
                var px = startX + (int)(particleTime * 15 + Math.Sin(seed + particleTime * 3) * 10);
                var py = startY - (int)(particleTime * 35);
                var drift = (int)(Math.Sin(particleTime * 4 + seed) * 6);
                
                var pSize = 1 + (int)(seed % 2);
                var colorIndex = (int)(seed % 3);
                var pColor = particleColors[colorIndex] * (alpha * particleAlpha * 0.7f * notification.GlowIntensity);
                
                if (py > bounds.Y && py < bounds.Bottom && px > bounds.X && px < bounds.Right)
                {
                    spriteBatch.Draw(pixel, new Rectangle(px + drift, py, pSize, pSize), pixel.SourceRect, pColor);
                }
            }
        }
    }
    
    private void DrawTimerBar(SpriteBatch spriteBatch, Rectangle bounds, AchievementNotification notification)
    {
        var pixel = Core.Graphics.PixelTexture;
        var alpha = Math.Min(notification.SlideProgress, 1f);
        
        // Timer bar at bottom
        var barHeight = 4;
        var barY = bounds.Bottom - 8;
        var barPadding = CornerSize + 4;
        var maxBarWidth = bounds.Width - barPadding * 2;
        
        // Background bar
        var bgColor = new Color(30, 25, 18) * alpha;
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + barPadding, barY, maxBarWidth, barHeight), pixel.SourceRect, bgColor);
        
        // Progress
        var progress = notification.TimeLeft / notification.TotalDuration;
        var barWidth = (int)(maxBarWidth * progress);
        
        // Color gradient based on remaining time
        Color barColor;
        if (progress > 0.5f)
        {
            barColor = new Color(220, 180, 60); // Gold
        }
        else if (progress > 0.25f)
        {
            var t = (progress - 0.25f) / 0.25f;
            barColor = Color.Lerp(new Color(220, 120, 40), new Color(220, 180, 60), t);
        }
        else
        {
            var t = progress / 0.25f;
            barColor = Color.Lerp(new Color(180, 60, 50), new Color(220, 120, 40), t);
        }
        
        barColor *= alpha;
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + barPadding, barY, barWidth, barHeight), pixel.SourceRect, barColor);
        
        // Bright leading edge
        if (barWidth > 3)
        {
            var edgeColor = new Color(255, 255, 220) * (alpha * 0.5f);
            spriteBatch.Draw(pixel, new Rectangle(bounds.X + barPadding + barWidth - 2, barY, 2, barHeight), pixel.SourceRect, edgeColor);
        }
        
        // Top highlight on bar
        var barHighlight = new Color(255, 240, 180) * (alpha * 0.3f);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X + barPadding, barY, barWidth, 1), pixel.SourceRect, barHighlight);
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

