using System.Globalization;
using System.IO;
using System.Text;

namespace Wendlewind.PawnLayout;

public static class BodyPartLayoutXml
{
    public static string Write(BodyDef body, int nativeSize, IEnumerable<(string PartKey, BodyPartLayoutData Data)> cells)
    {
        var directory = FindLayoutsDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, body.Moniker + ".xml");

        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine("<Definitions>");
        xml.AppendLine("    <BodyPartLayoutDef>");
        xml.AppendLine($"        <Moniker>{body.Moniker}BodyPartLayout</Moniker>");
        xml.AppendLine($"        <Label>{body.Label} Body Part Layout</Label>");
        xml.AppendLine($"        <Body>{body.Moniker}</Body>");
        xml.AppendLine($"        <NativeSize>{nativeSize}</NativeSize>");
        xml.AppendLine("        <Cells>");
        foreach (var (key, data) in cells)
        {
            xml.AppendLine("            <ListItem>");
            xml.AppendLine($"                <PartKey>{key}</PartKey>");
            xml.AppendLine($"                <PosX>{F(data.Position.X)}</PosX>");
            xml.AppendLine($"                <PosY>{F(data.Position.Y)}</PosY>");
            xml.AppendLine($"                <RenderOrder>{data.RenderOrder}</RenderOrder>");
            xml.AppendLine($"                <ScaleMultiplier>{F(data.ScaleMultiplier)}</ScaleMultiplier>");
            xml.AppendLine($"                <Rotation>{F(data.Rotation)}</Rotation>");
            xml.AppendLine($"                <FlipH>{data.FlipHorizontal.ToString().ToLowerInvariant()}</FlipH>");
            xml.AppendLine($"                <FlipV>{data.FlipVertical.ToString().ToLowerInvariant()}</FlipV>");
            if (data.EquipmentAttachment is { } attach)
            {
                xml.AppendLine("                <HasEquipmentAttachment>true</HasEquipmentAttachment>");
                xml.AppendLine($"                <EquipOffsetX>{F(attach.Offset.X)}</EquipOffsetX>");
                xml.AppendLine($"                <EquipOffsetY>{F(attach.Offset.Y)}</EquipOffsetY>");
                xml.AppendLine($"                <EquipRotation>{F(attach.Rotation)}</EquipRotation>");
                xml.AppendLine($"                <EquipScale>{F(attach.Scale)}</EquipScale>");
                xml.AppendLine($"                <EquipFlipH>{attach.FlipHorizontal.ToString().ToLowerInvariant()}</EquipFlipH>");
                xml.AppendLine($"                <EquipRenderWeapons>{attach.RenderWeapons.ToString().ToLowerInvariant()}</EquipRenderWeapons>");
                xml.AppendLine($"                <EquipRenderArmor>{attach.RenderArmor.ToString().ToLowerInvariant()}</EquipRenderArmor>");
            }

            xml.AppendLine("            </ListItem>");
        }

        xml.AppendLine("        </Cells>");
        xml.AppendLine("    </BodyPartLayoutDef>");
        xml.AppendLine("</Definitions>");
        File.WriteAllText(path, xml.ToString());
        return path;
    }

    private static string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    public static string FindLayoutsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Wendlewind", "Content", "Data", "Definitions", "Entities", "Pawns", "Bodies", "BodyPartLayouts");
            var parent = Path.GetDirectoryName(candidate);
            if (Directory.Exists(candidate) || (parent != null && Directory.Exists(parent)))
            {
                return candidate;
            }

            var client = Path.Combine(dir.FullName, "Content", "Data", "Definitions", "Entities", "Pawns", "Bodies", "BodyPartLayouts");
            var clientParent = Path.GetDirectoryName(client);
            if (Directory.Exists(client) || (clientParent != null && Directory.Exists(clientParent)))
            {
                return client;
            }

            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Content", "Data", "Definitions", "Entities", "Pawns", "Bodies", "BodyPartLayouts");
    }
}
