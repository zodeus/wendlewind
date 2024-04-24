using Grafted.Graphics.Textures;

namespace Grafted.Sim.Entities.Pawns;

public class RaceDef : Def {
    private Texture2D? _iconTexture;

    public PawnDef Species = null!;
    public string? TexturePath;
    //public DecisionPackageDef? DecisionPackage = null!;
    //public Type NameGeneratorClass = typeof(INameGenerator);
    //public List<SkillRecord> BaseSkills = new();
    //public List<TraitDef> Traits = null!;

    public Texture2D Icon => _iconTexture ??= TexturePath != null ? TextureUtils.PreMultiply(Core.Content.Load<Texture2D>(TexturePath))! : BaseContent.Textures.BadTexture;
}