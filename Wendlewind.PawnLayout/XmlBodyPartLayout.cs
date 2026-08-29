namespace Wendlewind.PawnLayout;

/// <summary>
/// Layout backed by an authored <see cref="BodyPartLayoutDef"/>. Keys are
/// <see cref="BodyPart.InternalLabel"/>, with a fallback to <see cref="Entity.Label"/>.
/// </summary>
public sealed class XmlBodyPartLayout : IBodyPartLayout
{
    private readonly Dictionary<string, BodyPartLayoutData> _byInternalLabel;
    private readonly Dictionary<string, BodyPartLayoutData> _byLabel;

    public int NativeSize { get; }

    public XmlBodyPartLayout(BodyPartLayoutDef def)
    {
        NativeSize = Math.Max(def.NativeSize, 1);
        _byInternalLabel = new Dictionary<string, BodyPartLayoutData>(StringComparer.Ordinal);
        _byLabel = new Dictionary<string, BodyPartLayoutData>(StringComparer.Ordinal);
        foreach (var cell in def.Cells)
        {
            var data = ToData(cell);
            if (!string.IsNullOrEmpty(cell.PartKey))
            {
                _byInternalLabel[cell.PartKey] = data;
                _byLabel.TryAdd(cell.PartKey, data);
            }
        }
    }

    public BodyPartLayoutData? GetLayoutData(BodyPart part)
    {
        if (_byInternalLabel.TryGetValue(part.InternalLabel, out var byInternal))
        {
            return byInternal;
        }

        if (_byLabel.TryGetValue(part.Label, out var byLabel))
        {
            return byLabel;
        }

        return null;
    }

    public BodyPartRenderInfo? GetRenderInfo(BodyPart part)
    {
        var layoutData = GetLayoutData(part);
        if (layoutData == null)
        {
            return null;
        }

        var icon = PawnTextures.GetIcon(part);
        return new BodyPartRenderInfo(icon, layoutData.Value);
    }

    private static BodyPartLayoutData ToData(BodyPartLayoutCell cell)
    {
        EquipmentAttachmentData? attachment = null;
        if (cell.HasEquipmentAttachment)
        {
            attachment = new EquipmentAttachmentData(
                new Vector2(cell.EquipOffsetX, cell.EquipOffsetY),
                cell.EquipRotation,
                cell.EquipScale,
                cell.EquipFlipH,
                cell.EquipRenderWeapons,
                cell.EquipRenderArmor);
        }

        return new BodyPartLayoutData(
            new Vector2(cell.PosX, cell.PosY),
            cell.RenderOrder,
            cell.ScaleMultiplier,
            cell.Rotation,
            cell.FlipH,
            cell.FlipV,
            attachment);
    }
}
