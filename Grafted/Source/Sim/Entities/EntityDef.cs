using System;
using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Graphics.Textures;
using Grafted.Sim.Gui.Widgets.EntityWidgets;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim.Entities;

public class EntityDef : Def {
    private Texture2D? _texture;
    private Texture2D? _iconTexture;

    public virtual EntityType EntityType => throw new NotImplementedException($"EntityType not set for class {GetType().Name}");
    public Type EntityClass = null!;
    public Type UiClass = typeof(EntityPanel);
    public MaterialType MaterialType = MaterialType.None;
    public List<BaseStat> BaseStats = new();
    public string? TexturePath;

    public virtual Texture2D Texture => _texture ??= TexturePath != null ? Core.Content.Load<Texture2D>(TexturePath) : BaseContent.Textures.BadTexture;
    public virtual Texture2D Icon => _iconTexture ??= TexturePath != null ? TextureUtils.PreMultiply(Texture)! : BaseContent.Textures.BadTexture;

    public EntityPanelBase UiPanelFor(Entity entity, EntityPanelProperties? properties = null) {
        return (EntityPanelBase) Activator.CreateInstance(UiClass, entity, properties)!;
    }
}