namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;
    
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
    public float Duration { get; set; }
    public float ElapsedTime { get; set; }
    public Vector2 MeasuredSize { get; set; }
    internal CombatFloaterStyle Style { get; set; } = CombatFloaterStyle.CreateRandom();

    public void Update(float deltaTime)
    {
        TimeLeft -= deltaTime;
        ElapsedTime += deltaTime;

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
                    Rng.Visual.Next(-20, 20),
                    Rng.Visual.Next(-10, 10)
                );
            }
            else
            {
                // Fallback to center with random offset
                position = new Vector2(nativeSize / 2f, nativeSize / 2f) + new Vector2(
                    Rng.Visual.Next(-50, 50),
                    Rng.Visual.Next(-50, 50)
                );
            }
        }
        else
        {
            // No body part specified, use center with random offset
            position = new Vector2(nativeSize / 2f, nativeSize / 2f) + new Vector2(
                Rng.Visual.Next(-50, 50),
                Rng.Visual.Next(-50, 50)
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
            Duration = duration,
            MeasuredSize = font.MeasureString(text),
            Style = CombatFloaterStyle.CreateRandom()
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
            var motion = text.Style.Evaluate(text.ElapsedTime, text.Duration);
            var screenPos = new Vector2(
                widgetBounds.X + text.Position.X * layoutScale,
                widgetBounds.Y + text.Position.Y * layoutScale
            ) + motion.Offset;

            var textSize = text.MeasuredSize * motion.Scale;
            var yOffset = -10;
            var textBounds = new Rectangle(
                (int)(screenPos.X - textSize.X / 2f),
                (int)(screenPos.Y - textSize.Y / 2f) + yOffset,
                (int)textSize.X,
                (int)textSize.Y);
            if (!widgetBounds.Intersects(textBounds))
                continue;

            CombatFloaterDraw.Draw(
                context,
                text.Font,
                text.Text,
                screenPos + new Vector2(0, yOffset),
                text.Color,
                text.Opacity * motion.Opacity,
                motion.Scale,
                text.MeasuredSize);
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

