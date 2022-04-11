using System.Collections.Generic;
using Grafted.Definitions;
using Grafted.Graphics.Textures;
using Grafted.Sim.Entities.Items;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim.Entities.Pawns;

public class RaceDef : Def {
    private Texture2D? _iconTexture;

    public PawnDef Species = null!;
    public List<ItemDef> Equipment = new();
    public BodyPartDef BodyParts = new();
    public string? TexturePath;
    //public DecisionPackageDef? DecisionPackage = null!;
    //public List<DropProperties> ItemDrops = new();

    //public Type NameGeneratorClass = typeof(INameGenerator);
    //public List<SkillRecord> BaseSkills = new();
    //public List<TraitDef> Traits = null!;

    public Texture2D Icon => _iconTexture ??= TexturePath != null ? TextureUtils.PreMultiply(Core.Content.Load<Texture2D>(TexturePath))! : BaseContent.Textures.BadTexture;
}