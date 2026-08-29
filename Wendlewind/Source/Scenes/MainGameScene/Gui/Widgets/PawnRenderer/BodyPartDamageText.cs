using Wendlewind.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;
    
/// <summary>
/// Represents a floating damage text that appears near a body part.
/// </summary>
public class BodyPartDamageText
{
    public string Text { get; set; } = "";
    public Color Color { get; set; } = Color.White;
    public Vector2 Position { get; set; }
    public DynamicSpriteFont Font { get; set; } = BaseContent.Fonts.Default.Normal;
    public float TimeLeft { get; set; }
    public float Scale { get; set; } = 1f;
    public float Opacity { get; set; } = 1f;
    public float VelocityY { get; set; } = -200f; // pixels per second
    public float VibrationAmplitude { get; set; } = 0.2f;
    public float Duration { get; set; }
    public float ElapsedTime { get; set; }

    public void Update(float deltaTime)
    {
        TimeLeft -= deltaTime;
        ElapsedTime += deltaTime;
        Position += new Vector2(0, VelocityY * deltaTime);

        // Fade out in the last 20% of lifetime
        var fadeThreshold = Duration * 0.2f;
        if (TimeLeft < fadeThreshold)
        {
            Opacity = TimeLeft / fadeThreshold;
        }
    }
}

/// <summary>
/// Manages and renders floating damage text for a pawn body widget.
/// </summary>
public class BodyPartDamageTextRenderer(IBodyPartLayout? layout, int nativeSize)
{
    private readonly List<BodyPartDamageText> _texts = new();

    private const float OverlapThreshold = 25f; // Minimum vertical distance between texts
    private const float StackOffset = 22f; // How much to offset when stacking
    
    /// <summary>
    /// Adds a damage text at the position of a body part.
    /// </summary>
    public void AddDamageText(BodyPart? bodyPart, string text, DynamicSpriteFont font, Color color, float duration = 2f)
    {
        Vector2 position;
        
        if (bodyPart != null && layout != null)
        {
            var renderInfo = layout.GetRenderInfo(bodyPart);
            if (renderInfo.HasValue)
            {
                // Position at the body part location with some random offset
                position = renderInfo.Value.Position + new Vector2(
                    Core.Random.Next(-20, 20),
                    Core.Random.Next(-10, 10)
                );
            }
            else
            {
                // Fallback to center with random offset
                position = new Vector2(nativeSize / 2f, nativeSize / 2f) + new Vector2(
                    Core.Random.Next(-50, 50),
                    Core.Random.Next(-50, 50)
                );
            }
        }
        else
        {
            // No body part specified, use center with random offset
            position = new Vector2(nativeSize / 2f, nativeSize / 2f) + new Vector2(
                Core.Random.Next(-50, 50),
                Core.Random.Next(-50, 50)
            );
        }
        
        // Offset position if it would overlap with existing texts
        position = FindNonOverlappingPosition(position);
        
        _texts.Add(new BodyPartDamageText
        {
            Font = font,
            Text = text,
            Color = color,
            Position = position,
            TimeLeft = duration,
            Duration = duration
        });
    }
    
    /// <summary>
    /// Finds a position that doesn't overlap with existing damage texts.
    /// Offsets vertically (upward) if needed.
    /// </summary>
    private Vector2 FindNonOverlappingPosition(Vector2 desiredPosition)
    {
        var position = desiredPosition;
        var maxIterations = 10; // Prevent infinite loops
        
        for (var i = 0; i < maxIterations; i++)
        {
            var hasOverlap = false;
            
            foreach (var existingText in _texts)
            {
                // Check if positions are too close (primarily vertically)
                var dx = Math.Abs(position.X - existingText.Position.X);
                var dy = Math.Abs(position.Y - existingText.Position.Y);
                
                // Only consider overlap if horizontally close and vertically overlapping
                if (dx < 40 && dy < OverlapThreshold)
                {
                    hasOverlap = true;
                    break;
                }
            }
            
            if (!hasOverlap)
            {
                return position;
            }
            
            // Offset upward to avoid overlap
            position.Y -= StackOffset;
        }
        
        return position;
    }
    
    /// <summary>
    /// Updates all damage texts.
    /// </summary>
    public void Update(float deltaTime)
    {
        for (var i = _texts.Count - 1; i >= 0; i--)
        {
            _texts[i].Update(deltaTime);
            if (_texts[i].TimeLeft <= 0)
            {
                _texts.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Renders all damage texts to the given render context.
    /// </summary>
    /// <param name="context">The render context.</param>
    /// <param name="widgetBounds">The bounds of the widget in screen coordinates.</param>
    /// <param name="layoutScale">Scale from native coordinates to widget coordinates.</param>
    public void Render(RenderContext context, Rectangle widgetBounds, float layoutScale)
    {
        foreach (var text in _texts)
        {
            // Convert native position to screen position
            var screenPos = new Vector2(
                widgetBounds.X + text.Position.X * layoutScale,
                widgetBounds.Y + text.Position.Y * layoutScale
            );
            
            // Add vibration effect (use elapsed time for consistent speed)
            var vibrationOffset = new Vector2(
                (float)(Math.Sin(text.ElapsedTime * 30) * text.VibrationAmplitude),
                (float)(Math.Cos(text.ElapsedTime * 42) * text.VibrationAmplitude * 0.5)
            );
            screenPos += vibrationOffset;
            
            var color = text.Color * text.Opacity;
            
            // Draw text with outline for visibility
            var textSize = text.Font.MeasureString(text.Text);
            var centeredPos = screenPos - textSize / 2f;

             var yOffset = -10;
            // Skip rendering if text is entirely outside widget bounds
            var textBounds = new Rectangle(
                (int)centeredPos.X, 
                (int)centeredPos.Y + yOffset, 
                (int)textSize.X, 
                (int)textSize.Y);
            if (!widgetBounds.Intersects(textBounds))
                continue;
            
            // Draw outline
            var outlineColor = Color.Black * text.Opacity * 0.8f;
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    context.DrawString(text.Font, text.Text, centeredPos + new Vector2(dx, dy), outlineColor);
                }
            }
            
            // Draw main text
            context.DrawString(text.Font, text.Text, centeredPos, color);
        }
    }
    
    /// <summary>
    /// Clears all damage texts.
    /// </summary>
    public void Clear()
    {
        _texts.Clear();
    }
}

