namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Layout data for positioning and transforming a body part in a layout.
/// </summary>
public readonly struct BodyPartLayoutData
{
    /// <summary>
    /// Base scale at ScaleOffset 0 (50% of original size).
    /// </summary>
    public const float BaseScale = 0.75f;
    
    /// <summary>
    /// The position to render this body part at (in native layout coordinates).
    /// </summary>
    public readonly Vector2 Position;
    
    /// <summary>
    /// The render order (lower = rendered first/behind).
    /// </summary>
    public readonly int RenderOrder;
    
    /// <summary>
    /// Scale offset for this body part. 0 = 50% size, 0.5 = 100% size, 1.0 = 150% size, etc.
    /// Final scale = 1f * ScaleMultiplier
    /// </summary>
    public readonly float ScaleMultiplier;
    
    /// <summary>
    /// Whether to flip the texture horizontally.
    /// </summary>
    public readonly bool FlipHorizontal;
    
    /// <summary>
    /// Whether to flip the texture vertically.
    /// </summary>
    public readonly bool FlipVertical;
    
    /// <summary>
    /// Gets the actual scale multiplier (1f * ScaleMultiplier).
    /// </summary>
    public float Scale => 1f * ScaleMultiplier;

    public BodyPartLayoutData(Vector2 position, int renderOrder, float scaleMultiplier = 1f, bool flipHorizontal = false, bool flipVertical = false)
    {
        Position = position;
        RenderOrder = renderOrder;
        ScaleMultiplier = scaleMultiplier;
        FlipHorizontal = flipHorizontal;
        FlipVertical = flipVertical;
    }
}

/// <summary>
/// Defines the texture and rendering information for a specific body part in a layout.
/// </summary>
public readonly struct BodyPartRenderInfo
{
    /// <summary>
    /// The texture to render for this body part.
    /// </summary>
    public readonly Texture2D Texture;
    
    /// <summary>
    /// The position to render this body part at (in native layout coordinates).
    /// </summary>
    public readonly Vector2 Position;
    
    /// <summary>
    /// The render order (lower = rendered first/behind).
    /// </summary>
    public readonly int RenderOrder;
    
    /// <summary>
    /// Scale multiplier for this body part (1.0 = original size).
    /// </summary>
    public readonly float Scale;
    
    /// <summary>
    /// Sprite effects for flipping (horizontal/vertical).
    /// </summary>
    public readonly SpriteEffects Effects;
    
    public BodyPartRenderInfo(Texture2D texture, Vector2 position, int renderOrder, float scale = 1f, SpriteEffects effects = SpriteEffects.None)
    {
        Texture = texture;
        Position = position;
        RenderOrder = renderOrder;
        Scale = scale;
        Effects = effects;
    }
    
    public BodyPartRenderInfo(Texture2D texture, BodyPartLayoutData layoutData)
    {
        Texture = texture;
        Position = layoutData.Position;
        RenderOrder = layoutData.RenderOrder;
        Scale = layoutData.Scale;
        
        Effects = SpriteEffects.None;
        if (layoutData.FlipHorizontal) Effects |= SpriteEffects.FlipHorizontally;
        if (layoutData.FlipVertical) Effects |= SpriteEffects.FlipVertically;
    }
}

/// <summary>
/// Interface for body-type-specific rendering layouts.
/// Different body types (human, boar, etc.) have different body part textures and arrangements.
/// </summary>
public interface IBodyPartLayout
{
    /// <summary>
    /// The native size of the body part textures (assumes square textures).
    /// </summary>
    int NativeSize { get; }
    
    /// <summary>
    /// Gets the render info for a specific body part, if available.
    /// </summary>
    /// <param name="part">The body part to get render info for.</param>
    /// <returns>The render info, or null if this part has no texture in this layout.</returns>
    BodyPartRenderInfo? GetRenderInfo(BodyPart part);
    
    /// <summary>
    /// Returns true if this layout supports the given pawn's body type.
    /// </summary>
    bool SupportsBody(PawnBody body);
}

