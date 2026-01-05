namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;

/// <summary>
/// Data for positioning equipment attached to a body part.
/// Offsets are relative to the body part's rendered center.
/// </summary>
public readonly struct EquipmentAttachmentData
{
    /// <summary>
    /// Offset from the body part's rendered center to the equipment attachment point.
    /// In native layout coordinates.
    /// </summary>
    public readonly Vector2 Offset;
    
    /// <summary>
    /// Rotation for attached equipment (in radians).
    /// </summary>
    public readonly float Rotation;
    
    /// <summary>
    /// Scale multiplier for attached equipment (1.0 = normal size).
    /// </summary>
    public readonly float Scale;
    
    /// <summary>
    /// Whether to flip the equipment horizontally.
    /// </summary>
    public readonly bool FlipHorizontal;
    
    /// <summary>
    /// Whether to render weapons at this attachment point.
    /// </summary>
    public readonly bool RenderWeapons;
    
    /// <summary>
    /// Whether to render armor at this attachment point.
    /// </summary>
    public readonly bool RenderArmor;
    
    public static readonly EquipmentAttachmentData Default = new(Vector2.Zero, 0f, 1f, false, true, true);
    
    public EquipmentAttachmentData(Vector2 offset, float rotation = 0f, float scale = 1f, bool flipHorizontal = false, bool renderWeapons = true, bool renderArmor = true)
    {
        Offset = offset;
        Rotation = rotation;
        Scale = scale;
        FlipHorizontal = flipHorizontal;
        RenderWeapons = renderWeapons;
        RenderArmor = renderArmor;
    }
}

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
    /// Rotation in radians.
    /// </summary>
    public readonly float Rotation;
    
    /// <summary>
    /// Whether to flip the texture horizontally.
    /// </summary>
    public readonly bool FlipHorizontal;
    
    /// <summary>
    /// Whether to flip the texture vertically.
    /// </summary>
    public readonly bool FlipVertical;
    
    /// <summary>
    /// Equipment attachment data for this body part (for weapons, etc.).
    /// Null means no equipment can be attached visually.
    /// </summary>
    public readonly EquipmentAttachmentData? EquipmentAttachment;
    
    /// <summary>
    /// Gets the actual scale multiplier (1f * ScaleMultiplier).
    /// </summary>
    public float Scale => 1f * ScaleMultiplier;

    public BodyPartLayoutData(Vector2 position, int renderOrder, float scaleMultiplier = 1f, float rotation = 0f, bool flipHorizontal = false, bool flipVertical = false, EquipmentAttachmentData? equipmentAttachment = null)
    {
        Position = position;
        RenderOrder = renderOrder;
        ScaleMultiplier = scaleMultiplier;
        Rotation = rotation;
        FlipHorizontal = flipHorizontal;
        FlipVertical = flipVertical;
        EquipmentAttachment = equipmentAttachment;
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
    /// Rotation in radians.
    /// </summary>
    public readonly float Rotation;
    
    /// <summary>
    /// Sprite effects for flipping (horizontal/vertical).
    /// </summary>
    public readonly SpriteEffects Effects;
    
    /// <summary>
    /// Equipment attachment data for this body part.
    /// Null means no equipment can be attached visually.
    /// </summary>
    public readonly EquipmentAttachmentData? EquipmentAttachment;
    
    public BodyPartRenderInfo(Texture2D texture, Vector2 position, int renderOrder, float scale = 1f, float rotation = 0f, SpriteEffects effects = SpriteEffects.None, EquipmentAttachmentData? equipmentAttachment = null)
    {
        Texture = texture;
        Position = position;
        RenderOrder = renderOrder;
        Scale = scale;
        Rotation = rotation;
        Effects = effects;
        EquipmentAttachment = equipmentAttachment;
    }
    
    public BodyPartRenderInfo(Texture2D texture, BodyPartLayoutData layoutData)
    {
        Texture = texture;
        Position = layoutData.Position;
        RenderOrder = layoutData.RenderOrder;
        Scale = layoutData.Scale;
        Rotation = layoutData.Rotation;
        EquipmentAttachment = layoutData.EquipmentAttachment;
        
        Effects = SpriteEffects.None;
        if (layoutData.FlipHorizontal) Effects |= SpriteEffects.FlipHorizontally;
        if (layoutData.FlipVertical) Effects |= SpriteEffects.FlipVertically;
    }
}

/// <summary>
/// Interface for body-type-specific rendering layouts.
/// Different body types (human, boar, etc.) have different body part textures and arrangements.
/// Layouts are associated with BodyDefs via the LayoutClass property.
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
}

/// <summary>
/// Shared helper for rendering body parts consistently across different renderers.
/// </summary>
public static class BodyPartRenderHelper
{
    /// <summary>
    /// Renders a single body part to the sprite batch.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to render to.</param>
    /// <param name="info">The render info for the body part.</param>
    /// <param name="position">Position override (in native coordinates), or null to use info.Position.</param>
    /// <param name="scale">Scale override, or null to use info.Scale.</param>
    /// <param name="rotation">Rotation override in radians, or null to use info.Rotation.</param>
    /// <param name="effects">Effects override, or null to use info.Effects.</param>
    /// <param name="layoutScale">Scale factor from native to render target size.</param>
    /// <param name="tint">Color tint to apply.</param>
    /// <param name="offset">Optional offset to add to the final position (in screen coordinates).</param>
    public static void RenderBodyPart(
        SpriteBatch spriteBatch,
        BodyPartRenderInfo info,
        Vector2? position = null,
        float? scale = null,
        float? rotation = null,
        SpriteEffects? effects = null,
        float layoutScale = 1f,
        Color? tint = null,
        Vector2? offset = null)
    {
        var pos = position ?? info.Position;
        var partScale = scale ?? info.Scale;
        var rot = rotation ?? info.Rotation;
        var fx = effects ?? info.Effects;
        var color = tint ?? Color.White;
        var off = offset ?? Vector2.Zero;
        
        var finalScale = partScale * layoutScale;
        var scaledPosition = pos * layoutScale;
        
        // Calculate origin - use center for rotation or flipping
        var origin = Vector2.Zero;
        if (fx != SpriteEffects.None || rot != 0f)
        {
            origin = new Vector2(info.Texture.Width / 2f, info.Texture.Height / 2f);
            scaledPosition += origin * finalScale;
        }
        
        var drawPosition = scaledPosition + off;
        
        spriteBatch.Draw(
            info.Texture,
            drawPosition,
            null,
            color,
            rot,
            origin,
            finalScale,
            fx,
            0f);
    }
    
    /// <summary>
    /// Renders equipped weapons for a body part. Call this BEFORE RenderBodyPart to have weapons appear behind the part.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to render to.</param>
    /// <param name="part">The body part to render weapons for.</param>
    /// <param name="partInfo">The render info for the body part.</param>
    /// <param name="position">Position override (in native coordinates), or null to use partInfo.Position.</param>
    /// <param name="scale">Scale override, or null to use partInfo.Scale.</param>
    /// <param name="equipmentAttachment">Equipment attachment override, or null to use partInfo.EquipmentAttachment.</param>
    /// <param name="layoutScale">Scale factor from native to render target size.</param>
    /// <param name="offset">Optional offset to add to the final position (in screen coordinates).</param>
    public static void RenderEquippedWeapons(
        SpriteBatch spriteBatch,
        BodyPart part,
        BodyPartRenderInfo partInfo,
        Vector2? position = null,
        float? scale = null,
        EquipmentAttachmentData? equipmentAttachment = null,
        float layoutScale = 1f,
        Vector2? offset = null)
    {
        var attachment = equipmentAttachment ?? partInfo.EquipmentAttachment ?? EquipmentAttachmentData.Default;
        
        // Don't render weapons if disabled for this attachment
        if (!attachment.RenderWeapons) return;
        
        var pos = position ?? partInfo.Position;
        var partScale = scale ?? partInfo.Scale;
        var off = offset ?? Vector2.Zero;
        
        foreach (var (slotType, item) in part.Equipment)
        {
            // Skip if no item equipped
            if (item == null) continue;
            
            // Skip built-in weapons (claws, teeth, etc.)
            if (slotType == EquipmentSlotType.BuiltIn) continue;
            
            // Only render weapons (HandWeapon, FootWeapon slots)
            if (item.ItemDef.EquipmentProperties?.EquipmentType != EquipmentType.Weapon) continue;
            
            // Get the weapon's texture
            var weaponTexture = item.ItemDef.Texture;
            if (weaponTexture == null) continue;
            
            // Calculate the center of the body part in screen coordinates
            var textureCenter = new Vector2(partInfo.Texture.Width / 2f, partInfo.Texture.Height / 2f);
            var finalScale = partScale * layoutScale;
            var partCenterScreen = pos * layoutScale + textureCenter * finalScale + off;
            
            // Apply the equipment attachment offset (scaled to screen coordinates)
            var screenPosition = partCenterScreen + attachment.Offset * layoutScale;
            var weaponRotation = attachment.Rotation;
            var weaponScale = attachment.Scale * layoutScale;
            var weaponEffects = attachment.FlipHorizontal ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            
            // Origin at bottom-center of weapon texture, so the grip aligns with the attachment point
            var origin = new Vector2(weaponTexture.Width / 2f, weaponTexture.Height);
            
            spriteBatch.Draw(
                weaponTexture,
                screenPosition,
                null,
                Color.White,
                weaponRotation,
                origin,
                weaponScale,
                weaponEffects,
                0f);
        }
    }
    
    /// <summary>
    /// Renders equipped armor for a body part. Call this AFTER RenderBodyPart to have armor appear on top of the part.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to render to.</param>
    /// <param name="part">The body part to render armor for.</param>
    /// <param name="partInfo">The render info for the body part.</param>
    /// <param name="position">Position override (in native coordinates), or null to use partInfo.Position.</param>
    /// <param name="scale">Scale override, or null to use partInfo.Scale.</param>
    /// <param name="equipmentAttachment">Equipment attachment override, or null to use partInfo.EquipmentAttachment.</param>
    /// <param name="layoutScale">Scale factor from native to render target size.</param>
    /// <param name="offset">Optional offset to add to the final position (in screen coordinates).</param>
    public static void RenderEquippedArmor(
        SpriteBatch spriteBatch,
        BodyPart part,
        BodyPartRenderInfo partInfo,
        Vector2? position = null,
        float? scale = null,
        EquipmentAttachmentData? equipmentAttachment = null,
        float layoutScale = 1f,
        Vector2? offset = null)
    {
        var attachment = equipmentAttachment ?? partInfo.EquipmentAttachment;
        
        // Don't render armor if no equipment attachment is defined or armor rendering is disabled
        if (attachment == null || !attachment.Value.RenderArmor) return;
        
        var pos = position ?? partInfo.Position;
        var partScale = scale ?? partInfo.Scale;
        var off = offset ?? Vector2.Zero;
        
        foreach (var (slotType, item) in part.Equipment)
        {
            // Skip if no item equipped
            if (item == null) continue;
            
            // Skip built-in equipment (claws, teeth, etc.)
            if (slotType == EquipmentSlotType.BuiltIn) continue;
            
            // Only render armor
            if (item.ItemDef.EquipmentProperties?.EquipmentType != EquipmentType.Armor) continue;
            
            // Get the armor's texture
            var armorTexture = item.ItemDef.Texture;
            if (armorTexture == null) continue;
            
            // Calculate the center of the body part in screen coordinates
            var textureCenter = new Vector2(partInfo.Texture.Width / 2f, partInfo.Texture.Height / 2f);
            var finalScale = partScale * layoutScale;
            var partCenterScreen = pos * layoutScale + textureCenter * finalScale + off;
            
            // Apply the equipment attachment offset (scaled to screen coordinates)
            var screenPosition = partCenterScreen + attachment.Value.Offset * layoutScale;
            var armorRotation = attachment.Value.Rotation;
            var armorScale = attachment.Value.Scale * layoutScale;
            var armorEffects = attachment.Value.FlipHorizontal ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            
            // Calculate armor origin (center of armor texture)
            var armorOrigin = new Vector2(armorTexture.Width / 2f, armorTexture.Height / 2f);
            
            spriteBatch.Draw(
                armorTexture,
                screenPosition,
                null,
                Color.White,
                armorRotation,
                armorOrigin,
                armorScale,
                armorEffects,
                0f);
        }
    }
}

