namespace Grafted.Sim.Entities.Pawns;

public class BodyEffectDef : Def
{
    private Texture2D? _texture;
    public string? TexturePath;
    public List<AffectedStatRecord>? AffectedStats;
    public string? Notes;
    public virtual Texture2D Texture => _texture ??= TexturePath != null ? Core.Content.Load<Texture2D>(TexturePath) : BaseContent.Textures.BadTexture;
}

public class BodyStanceDef : Def
{
    private Texture2D? _texture;
    public string? TexturePath;
    public List<AffectedStatRecord>? AffectedStats;
    public string? Notes;
    public virtual Texture2D Texture => _texture ??= TexturePath != null ? Core.Content.Load<Texture2D>(TexturePath) : BaseContent.Textures.BadTexture;

    public void ModifyStatIfApplicable(StatDef stat, ref float value)
    {
        if (AffectedStats == null)
        {
            return;
        }

        foreach (var affectedStat in AffectedStats)
        {
            if (affectedStat.Stat != stat)
            {
                continue;
            }

            if (affectedStat.Factor != null)
            {
                value += (value * affectedStat.Factor.Value);
            }

            if (affectedStat.Offset != null)
            {
                value += affectedStat.Offset.Value;
            }
        }
    }
}