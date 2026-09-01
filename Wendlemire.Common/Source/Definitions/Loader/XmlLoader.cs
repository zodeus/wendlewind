using System.IO;
using System.Xml;

namespace Wendlemire.Definitions.Loader;

public static class XmlLoader
{
    private static readonly List<Def> Defs = new();
    private const string DataListName = "Definitions";

    public static void LoadXmlAssetsIntoMemory(string directory)
    {
        IEnumerable<XmlAsset> list = LoadXmlFiles(directory);

        Dictionary<XmlNode, XmlAsset> assetByNodes = new();
        var xmlDocument = MergeFilesIntoSingleDocument(list, assetByNodes);

        ProcessDocument(xmlDocument, assetByNodes);
        RegisterDefsWithDatabase();
    }

    private static void RegisterDefsWithDatabase()
    {
        foreach (var defType in typeof(Def).Subclasses())
        {
            GenericHelpers.InvokeStaticMethodOnGenericType(typeof(DefRepository<>), defType, "AddFiltered", Defs);
        }
    }

    private static IEnumerable<XmlAsset> LoadXmlFiles(string dataDirectory)
    {
        FileInfo[] files = new DirectoryInfo(dataDirectory).GetFiles("*.xml", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            yield return new XmlAsset(File.ReadAllText(file.FullName));
        }
    }

    private static XmlDocument MergeFilesIntoSingleDocument(IEnumerable<XmlAsset> list, Dictionary<XmlNode, XmlAsset> assetByNodes)
    {
        var xmlDocument = new XmlDocument();
        xmlDocument.AppendChild(xmlDocument.CreateElement(DataListName));
        foreach (var xml in list)
        {
            foreach (XmlNode? childNode in xml.Document.DocumentElement!.ChildNodes)
            {
                if (childNode == null)
                {
                    continue;
                }

                var node = xmlDocument.ImportNode(childNode, true);
                assetByNodes[node] = xml;
                xmlDocument.DocumentElement!.AppendChild(node);
            }
        }

        return xmlDocument;
    }

    private static void ProcessDocument(XmlDocument root, Dictionary<XmlNode, XmlAsset> assetByNodes)
    {
        List<XmlNode> rootDefNodes = new();
        foreach (XmlNode? item in root.DocumentElement!.ChildNodes)
        {
            if (item == null)
            {
                continue;
            }

            rootDefNodes.Add(item);
            if (item.NodeType == XmlNodeType.Element)
            {
                //assetByNodes.TryGetValue(item, out XmlAsset? value);
                XmlInheritance.TryRegister(item);
            }
        }

        XmlInheritance.Resolve();

        foreach (var node in rootDefNodes)
        {
            var def = DeserializeDef(node);
            if (def == null)
            {
                continue;
            }

            Defs.Add(def);
        }
    }

    private static Def? DeserializeDef(XmlNode node)
    {
        if (node.NodeType != XmlNodeType.Element)
        {
            Log.Error($"Failed to deserialize Def: {node.OuterXml}");
            return null;
        }

        var xmlAttribute = node.Attributes?["Abstract"];
        if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
        {
            return null;
        }

        //todo support assemblies
        var type = GenTypes.GetTypeInAnyAssembly(node.Name);
        if (type == null || !typeof(Def).IsAssignableFrom(type))
        {
            Log.Error($"Failed to deserialize Def: {node.OuterXml}");
            return null;
        }

        var def = (Def?)GenericHelpers.InvokeStaticGenericMethod(typeof(DirectXmlToObject), type, DirectXmlToObject.ObjectFromXmlMethodName, node, true);
        return def;
    }
}

public class XmlAsset
{
    public XmlDocument Document { get; }
    public XmlReader Reader { get; }

    public XmlAsset(string content)
    {
        var settings = new XmlReaderSettings
        {
            IgnoreComments = true, IgnoreWhitespace = true, CheckCharacters = false
        };
        using var input = new StringReader(content);
        Reader = XmlReader.Create(input, settings);
        Document = new XmlDocument();
        Document.Load(Reader);
    }
}