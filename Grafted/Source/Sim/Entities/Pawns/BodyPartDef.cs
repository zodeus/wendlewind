using Grafted.Graphics.Textures;

namespace Grafted.Sim.Entities.Pawns;

public class BodyPartDef : EntityDef {
    private Texture2D? _whiteIconTexture;

    public override EntityType EntityType => EntityType.BodyPart;
    //public override Type DefUiClass => typeof(ItemDefPanel);
    public BodyPartType BodyPartType = BodyPartType.Undefined;
    public float BloodAmount = 0;
    public float HitWeight = 0;
    public bool IsVital = false;
    public bool IsOrgan = false;
    public bool IsFlesh = false;
    public bool IsBone = false;
    public bool IsExoskeleton = false;
    public float MobilityFraction = 0;
    public List<BodyPartSocketDef> Sockets = new();
    public List<EquipmentSlotType>? EquipmentSlots = null;
    public AdaptiveBodyPartProperties? AdaptiveProperties;
    public string? WhiteIconTexturePath;

    public virtual Texture2D WhiteIcon => _whiteIconTexture ??= WhiteIconTexturePath != null ? TextureUtils.PreMultiply(Core.Content.Load<Texture2D>($"{WhiteIconTexturePath}"))! : Icon;
}