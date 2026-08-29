using System.Globalization;
using System.Text;
using System.Xml;
using Num = System.Numerics;

namespace Wendlewind.Editor;

public sealed class EquipmentGridEditor
{
    private const string DragPayloadId = "EQ_SLOT";
    private const float CellSize = 108f;
    private const float CellGap = 10f;

    private readonly GameContext _context;
    private readonly List<BodyDef> _bodies;
    private int _selectedBody;
    private Pawn? _pawn;
    private string? _status;
    private bool _dirty;
    private int _columns = 8;
    private int _rows = 10;
    private readonly List<SlotToken> _allSlots = [];
    private readonly Dictionary<(string PartKey, EquipmentSlotType Slot), (int Col, int Row)> _placed = new();

    public EquipmentGridEditor(GameContext context)
    {
        _context = context;
        _bodies = DefRepository<BodyDef>.Defs.OrderBy(b => b.Label).ToList();
        var human = _bodies.FindIndex(b => b.Moniker == "HumanBody");
        _selectedBody = human >= 0 ? human : 0;
        if (_bodies.Count > 0)
        {
            LoadBody(_bodies[_selectedBody]);
        }
    }

    public void Draw()
    {
        var display = ImGui.GetIO().DisplaySize;
        ImGui.SetNextWindowPos(Num.Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(display, ImGuiCond.Always);
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBringToFrontOnFocus;
        if (!ImGui.Begin("Equipment Grid Editor", flags))
        {
            ImGui.End();
            return;
        }

        DrawToolbar();
        ImGui.Separator();
        var statusHeight = ImGui.GetFrameHeightWithSpacing() + 8;
        ImGui.BeginChild("workspace", new Num.Vector2(0, -statusHeight), ImGuiChildFlags.None);
        if (ImGui.BeginTable("split", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("palette", ImGuiTableColumnFlags.WidthFixed, 360);
            ImGui.TableSetupColumn("grid", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextColumn();
            DrawPalette();
            ImGui.TableNextColumn();
            DrawGrid();
            ImGui.EndTable();
        }

        ImGui.EndChild();
        ImGui.Separator();
        DrawStatusBar();
        ImGui.End();
    }

    private void DrawToolbar()
    {
        var labels = _bodies.Select(b => b.Label).ToArray();
        var previous = _selectedBody;
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Body");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(280);
        if (ImGui.Combo("##body", ref _selectedBody, labels, labels.Length) && _selectedBody != previous)
        {
            LoadBody(_bodies[_selectedBody]);
        }

        ImGui.SameLine();
        if (ImGui.Button("Reload", new Num.Vector2(110, 0)))
        {
            LoadBody(_bodies[_selectedBody]);
            _status = "Reloaded from disk.";
            _dirty = false;
        }

        ImGui.SameLine();
        if (ImGui.Button("Save", new Num.Vector2(110, 0)))
        {
            Save();
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Columns");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(110);
        var columns = _columns;
        if (ImGui.InputInt("##columns", ref columns))
        {
            Resize(Math.Clamp(columns, 1, 24), _rows);
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Rows");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(110);
        var rows = _rows;
        if (ImGui.InputInt("##rows", ref rows))
        {
            Resize(_columns, Math.Clamp(rows, 1, 24));
        }

        if (_dirty)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Num.Vector4(0.95f, 0.75f, 0.3f, 1f), "Unsaved changes");
        }
    }

    private void DrawPalette()
    {
        var unplaced = UnplacedSlots();
        ImGui.Text($"Unplaced slots  ({unplaced.Count})");
        ImGui.TextDisabled("Drag onto the grid. Drop here to remove.");
        ImGui.BeginChild("palette-list", new Num.Vector2(0, 0), ImGuiChildFlags.Borders);

        if (unplaced.Count == 0)
        {
            ImGui.Dummy(new Num.Vector2(1, 8));
            ImGui.TextDisabled("All slots are on the grid.");
        }

        foreach (var slot in unplaced)
        {
            DrawPaletteItem(slot);
        }

        ImGui.EndChild();
        if (ImGui.BeginDragDropTarget())
        {
            if (TryAcceptSlotIndex(out var index))
            {
                Unplace(index);
            }

            ImGui.EndDragDropTarget();
        }
    }

    private void DrawPaletteItem(SlotToken slot)
    {
        var index = IndexOf(slot);
        var color = SlotColor(slot.Slot);
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Brighten(color, 0.12f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Brighten(color, 0.18f));
        ImGui.Button($"{slot.PartLabel}\n{PrettySlot(slot.Slot)}##pal{index}", new Num.Vector2(-1, 72));
        ImGui.PopStyleColor(3);
        BeginSlotDrag(index, slot);
    }

    private void DrawGrid()
    {
        ImGui.Text("Grid");
        ImGui.SameLine();
        ImGui.TextDisabled("Drag slots to move. Right-click a cell to clear it.");
        ImGui.BeginChild("grid-scroll", new Num.Vector2(0, 0), ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar);

        var occupied = _placed.ToDictionary(p => p.Value, p => p.Key);
        var step = CellSize + CellGap;
        var origin = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Num.Vector2(_columns * step - CellGap, _rows * step - CellGap));
        var draw = ImGui.GetWindowDrawList();

        for (var row = 0; row < _rows; row++)
        {
            for (var col = 0; col < _columns; col++)
            {
                var min = new Num.Vector2(origin.X + col * step, origin.Y + row * step);
                var max = min + new Num.Vector2(CellSize, CellSize);
                var hasSlot = occupied.TryGetValue((col, row), out var token);
                var slot = hasSlot ? FindToken(token) : default;

                ImGui.SetCursorScreenPos(min);
                ImGui.InvisibleButton($"cell-{col}-{row}", new Num.Vector2(CellSize, CellSize));
                var hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    ImGui.SetTooltip(hasSlot ? slot.Display : $"Empty  ({col}, {row})");
                }

                if (hasSlot)
                {
                    BeginSlotDrag(IndexOf(slot), slot);
                }

                var dropPreview = false;
                if (ImGui.BeginDragDropTarget())
                {
                    dropPreview = true;
                    if (TryAcceptSlotIndex(out var index))
                    {
                        PlaceOrSwap(index, col, row);
                    }

                    ImGui.EndDragDropTarget();
                }

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && hasSlot)
                {
                    _placed.Remove(token);
                    MarkDirty();
                }

                var fill = hasSlot ? SlotColor(slot.Slot) : new Num.Vector4(0.16f, 0.15f, 0.14f, 1f);
                if (hovered || dropPreview)
                {
                    fill = Brighten(fill, 0.10f);
                }

                var border = dropPreview
                    ? new Num.Vector4(0.95f, 0.82f, 0.40f, 1f)
                    : new Num.Vector4(0.55f, 0.42f, 0.24f, 1f);
                draw.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(fill), 6f);
                draw.AddRect(min, max, ImGui.ColorConvertFloat4ToU32(border), 6f, ImDrawFlags.None, dropPreview ? 3f : 2f);

                if (hasSlot)
                {
                    var textColor = ImGui.ColorConvertFloat4ToU32(new Num.Vector4(0.98f, 0.94f, 0.82f, 1f));
                    var subColor = ImGui.ColorConvertFloat4ToU32(new Num.Vector4(0.86f, 0.78f, 0.58f, 1f));
                    draw.AddText(min + new Num.Vector2(10, 14), textColor, slot.PartLabel);
                    draw.AddText(min + new Num.Vector2(10, 42), subColor, PrettySlot(slot.Slot));
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawStatusBar()
    {
        var placed = _placed.Count;
        var total = _allSlots.Count;
        ImGui.Text($"{placed} / {total} slots placed");
        ImGui.SameLine();
        if (!string.IsNullOrEmpty(_status))
        {
            ImGui.TextColored(new Num.Vector4(0.85f, 0.75f, 0.4f, 1f), _status);
        }
    }

    private static void BeginSlotDrag(int index, SlotToken slot)
    {
        if (!ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceAllowNullID))
        {
            return;
        }

        SetSlotPayload(index);
        ImGui.Text($"{slot.PartLabel}  /  {PrettySlot(slot.Slot)}");
        ImGui.EndDragDropSource();
    }

    private static unsafe void SetSlotPayload(int index)
    {
        ImGui.SetDragDropPayload(DragPayloadId, (IntPtr)(&index), sizeof(int));
    }

    private static unsafe bool TryAcceptSlotIndex(out int index)
    {
        index = -1;
        var payload = ImGui.AcceptDragDropPayload(DragPayloadId);
        if (payload.NativePtr == null || !payload.IsDelivery() || payload.DataSize < sizeof(int))
        {
            return false;
        }

        index = *(int*)payload.Data;
        return true;
    }

    private void PlaceOrSwap(int index, int col, int row)
    {
        if (index < 0 || index >= _allSlots.Count)
        {
            return;
        }

        var incoming = _allSlots[index];
        var incomingKey = (incoming.PartKey, incoming.Slot);
        var occupant = _placed.FirstOrDefault(p => p.Value == (col, row));
        if (occupant.Key != default && !occupant.Key.Equals(incomingKey))
        {
            if (_placed.TryGetValue(incomingKey, out var from))
            {
                _placed[occupant.Key] = from;
            }
            else
            {
                _placed.Remove(occupant.Key);
            }
        }

        _placed[incomingKey] = (col, row);
        MarkDirty();
        _status = null;
    }

    private void Unplace(int index)
    {
        if (index < 0 || index >= _allSlots.Count)
        {
            return;
        }

        var slot = _allSlots[index];
        if (_placed.Remove((slot.PartKey, slot.Slot)))
        {
            MarkDirty();
            _status = $"Removed {slot.Display} from the grid.";
        }
    }

    private void Resize(int columns, int rows)
    {
        if (columns == _columns && rows == _rows)
        {
            return;
        }

        _columns = columns;
        _rows = rows;
        var evicted = _placed
            .Where(p => p.Value.Col >= columns || p.Value.Row >= rows)
            .Select(p => p.Key)
            .ToList();
        foreach (var key in evicted)
        {
            _placed.Remove(key);
        }

        MarkDirty();
    }

    private void MarkDirty()
    {
        _dirty = true;
    }

    private List<SlotToken> UnplacedSlots() =>
        _allSlots.Where(s => !_placed.ContainsKey((s.PartKey, s.Slot))).ToList();

    private int IndexOf(SlotToken slot) =>
        _allSlots.FindIndex(s => s.PartKey == slot.PartKey && s.Slot == slot.Slot);

    private SlotToken FindToken((string PartKey, EquipmentSlotType Slot) key) =>
        _allSlots.First(s => s.PartKey == key.PartKey && s.Slot == key.Slot);

    private static string PrettySlot(EquipmentSlotType slot) => slot switch
    {
        EquipmentSlotType.PotionSlot1 => "Potion 1",
        EquipmentSlotType.PotionSlot2 => "Potion 2",
        EquipmentSlotType.HandWeapon => "Weapon",
        EquipmentSlotType.FootWeapon => "Foot weapon",
        EquipmentSlotType.HandArmor => "Hand armor",
        EquipmentSlotType.FootArmor => "Foot armor",
        EquipmentSlotType.LegArmor => "Leg armor",
        EquipmentSlotType.ArmArmor => "Arm armor",
        EquipmentSlotType.TorsoArmor => "Torso armor",
        EquipmentSlotType.NeckArmor => "Neck armor",
        EquipmentSlotType.HeadArmor => "Head armor",
        _ => slot.ToString()
    };

    private static Num.Vector4 SlotColor(EquipmentSlotType slot) => slot switch
    {
        EquipmentSlotType.HandWeapon or EquipmentSlotType.FootWeapon => new Num.Vector4(0.48f, 0.26f, 0.16f, 1f),
        EquipmentSlotType.PotionSlot1 or EquipmentSlotType.PotionSlot2 => new Num.Vector4(0.22f, 0.38f, 0.24f, 1f),
        EquipmentSlotType.Bag => new Num.Vector4(0.36f, 0.28f, 0.18f, 1f),
        EquipmentSlotType.BuiltIn => new Num.Vector4(0.24f, 0.24f, 0.28f, 1f),
        EquipmentSlotType.Cloak or EquipmentSlotType.Necklace => new Num.Vector4(0.32f, 0.26f, 0.40f, 1f),
        _ => new Num.Vector4(0.42f, 0.32f, 0.18f, 1f)
    };

    private static Num.Vector4 Brighten(Num.Vector4 color, float amount) =>
        new(Math.Clamp(color.X + amount, 0f, 1f), Math.Clamp(color.Y + amount, 0f, 1f), Math.Clamp(color.Z + amount, 0f, 1f), color.W);

    private void LoadBody(BodyDef body)
    {
        _pawn = CreatePawn(body);
        _allSlots.Clear();
        _placed.Clear();
        _status = null;
        _dirty = false;

        if (_pawn == null)
        {
            _status = $"No pawn uses {body.Label}.";
            return;
        }

        foreach (var (part, slots) in _pawn.Equipment.Slots)
        {
            if (slots.Count == 0 || EquipmentGridLayout.IsHiddenPart(part))
            {
                continue;
            }

            foreach (var slot in slots)
            {
                _allSlots.Add(new SlotToken(part.InternalLabel, part.Label, slot));
            }
        }

        var authored = TryReadXml(body) ?? EquipmentGridDef.ForBody(body);
        if (authored == null)
        {
            _columns = 8;
            _rows = 10;
            return;
        }

        _columns = Math.Max(authored.Columns, 1);
        _rows = Math.Max(authored.Rows, 1);
        var known = _allSlots.Select(s => (s.PartKey, s.Slot)).ToHashSet();
        foreach (var cell in authored.Cells)
        {
            var key = (cell.PartKey, cell.Slot);
            if (!known.Contains(key))
            {
                continue;
            }

            _placed[key] = (cell.Col, cell.Row);
        }
    }

    private Pawn? CreatePawn(BodyDef body)
    {
        var pawnDef = DefRepository<PawnDef>.Defs.FirstOrDefault(p => p.Body == body);
        if (pawnDef == null)
        {
            return null;
        }

        var loadout = DefRepository<PawnLoadoutDef>.GetByMoniker("EmptyLoadout", raiseError: false)
                      ?? Defs.PawnLoadouts.DefaultStarterLoadout;
        return PawnGenerator.CreatePawn(_context, new PawnRequest("Editor", pawnDef, loadout, PawnType.Player));
    }

    private void Save()
    {
        if (_bodies.Count == 0)
        {
            return;
        }

        var body = _bodies[_selectedBody];
        var path = Path.Combine(FindGridsDirectory(), body.Moniker + ".xml");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var moniker = body.Moniker + "EquipmentGrid";
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine("<Definitions>");
        xml.AppendLine("    <EquipmentGridDef>");
        xml.AppendLine($"        <Moniker>{moniker}</Moniker>");
        xml.AppendLine($"        <Label>{body.Label} Equipment Grid</Label>");
        xml.AppendLine($"        <Body>{body.Moniker}</Body>");
        xml.AppendLine($"        <Columns>{_columns}</Columns>");
        xml.AppendLine($"        <Rows>{_rows}</Rows>");
        xml.AppendLine("        <Cells>");
        foreach (var slot in _allSlots)
        {
            if (!_placed.TryGetValue((slot.PartKey, slot.Slot), out var cell))
            {
                continue;
            }

            xml.AppendLine("            <ListItem>");
            xml.AppendLine($"                <PartKey>{slot.PartKey}</PartKey>");
            xml.AppendLine($"                <Slot>{slot.Slot}</Slot>");
            xml.AppendLine($"                <Col>{cell.Col}</Col>");
            xml.AppendLine($"                <Row>{cell.Row}</Row>");
            xml.AppendLine("            </ListItem>");
        }

        xml.AppendLine("        </Cells>");
        xml.AppendLine("    </EquipmentGridDef>");
        xml.AppendLine("</Definitions>");
        File.WriteAllText(path, xml.ToString());
        _dirty = false;
        _status = $"Saved {body.Moniker}.xml";
    }

    private static EquipmentGridDef? TryReadXml(BodyDef body)
    {
        var path = Path.Combine(FindGridsDirectory(), body.Moniker + ".xml");
        if (!File.Exists(path))
        {
            return null;
        }

        var document = new XmlDocument();
        document.Load(path);
        var root = document.SelectSingleNode("/Definitions/EquipmentGridDef");
        if (root == null)
        {
            return null;
        }

        var def = new EquipmentGridDef
        {
            Moniker = root["Moniker"]?.InnerText ?? body.Moniker + "EquipmentGrid",
            Label = root["Label"]?.InnerText ?? body.Label,
            Body = body,
            Columns = ParseInt(root["Columns"]?.InnerText, 8),
            Rows = ParseInt(root["Rows"]?.InnerText, 10)
        };

        var cells = root["Cells"];
        if (cells == null)
        {
            return def;
        }

        foreach (XmlNode node in cells.ChildNodes)
        {
            if (node.Name != "ListItem")
            {
                continue;
            }

            if (!Enum.TryParse<EquipmentSlotType>(node["Slot"]?.InnerText, out var slot))
            {
                continue;
            }

            def.Cells.Add(new EquipmentGridCell
            {
                PartKey = node["PartKey"]?.InnerText ?? "",
                Slot = slot,
                Col = ParseInt(node["Col"]?.InnerText, 0),
                Row = ParseInt(node["Row"]?.InnerText, 0)
            });
        }

        return def;
    }

    private static int ParseInt(string? text, int fallback) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static string FindGridsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Wendlewind", "Content", "Data", "Definitions", "Entities", "Pawns", "Bodies", "EquipmentGrids");
            if (Directory.Exists(candidate) || Directory.Exists(Path.GetDirectoryName(candidate)!))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Content", "Data", "Definitions", "Entities", "Pawns", "Bodies", "EquipmentGrids");
    }

    private readonly record struct SlotToken(string PartKey, string PartLabel, EquipmentSlotType Slot)
    {
        public string Display => $"{PartLabel}  /  {PrettySlot(Slot)}";
    }
}
