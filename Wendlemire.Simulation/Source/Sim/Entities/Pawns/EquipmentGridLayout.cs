namespace Wendlemire.Sim.Entities.Pawns;

/// <summary>
/// Resolves a pawn's equipment slots onto a grid. Uses an authored
/// <see cref="EquipmentGridDef"/> when one exists for the body; otherwise
/// stacks slots in linear columns.
/// </summary>
public sealed class EquipmentGridLayout
{
    public IReadOnlyDictionary<(BodyPart Part, EquipmentSlotType Slot), (int Col, int Row)> Slots { get; }
    public int Columns { get; }
    public int Rows { get; }

    private EquipmentGridLayout(
        Dictionary<(BodyPart Part, EquipmentSlotType Slot), (int Col, int Row)> slots,
        int columns,
        int rows)
    {
        Slots = slots;
        Columns = columns;
        Rows = rows;
    }

    public static bool IsHiddenPart(BodyPart part) =>
        part.Type is BodyPartType.Finger or BodyPartType.Thumb or BodyPartType.Eye;

    public static EquipmentGridLayout Build(Pawn pawn)
    {
        var parts = new List<(BodyPart Part, List<EquipmentSlotType> Slots)>();
        foreach (var (bodyPart, slots) in pawn.Equipment.Slots)
        {
            if (slots.Count == 0 || IsHiddenPart(bodyPart))
            {
                continue;
            }

            parts.Add((bodyPart, slots));
        }

        var authored = EquipmentGridDef.ForBody(pawn.Body.Def);
        if (authored != null)
        {
            return BuildFromAuthored(parts, authored);
        }

        return BuildLinear(parts);
    }

    private static EquipmentGridLayout BuildFromAuthored(
        IReadOnlyList<(BodyPart Part, List<EquipmentSlotType> Slots)> parts,
        EquipmentGridDef authored)
    {
        var byInternalLabel = new Dictionary<string, BodyPart>(StringComparer.Ordinal);
        var byLabel = new Dictionary<string, BodyPart>(StringComparer.Ordinal);
        var liveSlots = new HashSet<(BodyPart Part, EquipmentSlotType Slot)>();

        foreach (var (part, partSlots) in parts)
        {
            byInternalLabel[part.InternalLabel] = part;
            byLabel.TryAdd(part.Label, part);
            foreach (var slot in partSlots)
            {
                liveSlots.Add((part, slot));
            }
        }

        var slots = new Dictionary<(BodyPart Part, EquipmentSlotType Slot), (int Col, int Row)>();
        foreach (var cell in authored.Cells)
        {
            if (!byInternalLabel.TryGetValue(cell.PartKey, out var part) &&
                !byLabel.TryGetValue(cell.PartKey, out part))
            {
                continue;
            }

            var key = (part, cell.Slot);
            if (!liveSlots.Contains(key))
            {
                continue;
            }

            slots[key] = (cell.Col, cell.Row);
        }

        return new EquipmentGridLayout(
            slots,
            Math.Max(authored.Columns, 1),
            Math.Max(authored.Rows, 1));
    }

    private static EquipmentGridLayout BuildLinear(IReadOnlyList<(BodyPart Part, List<EquipmentSlotType> Slots)> parts)
    {
        var slots = new Dictionary<(BodyPart Part, EquipmentSlotType Slot), (int Col, int Row)>();
        var maxRows = 1;
        var col = 0;

        foreach (var (part, partSlots) in parts)
        {
            var row = 0;
            foreach (var slot in partSlots)
            {
                slots[(part, slot)] = (col, row++);
            }

            maxRows = Math.Max(maxRows, Math.Max(row, 1));
            col++;
        }

        return new EquipmentGridLayout(slots, Math.Max(col, 1), maxRows);
    }
}
