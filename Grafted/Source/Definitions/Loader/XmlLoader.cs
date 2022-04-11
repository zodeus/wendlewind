using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Grafted.Definitions.Loader.Simulation.Persistence;
using Grafted.Utils;

namespace Grafted.Definitions.Loader;

public static class XmlLoader {
    private static readonly List<Def> Defs = new();
    private const string DataListName = "Definitions";

    public static void LoadXmlAssetsIntoMemory(string directory) {
        IEnumerable<XmlAsset> list = LoadXmlFiles(directory);

        Dictionary<XmlNode, XmlAsset> assetByNodes = new();
        XmlDocument xmlDocument = MergeFilesIntoSingleDocument(list, assetByNodes);

        ProcessDocument(xmlDocument, assetByNodes);
        //todo ResolveInheritance
        RegisterDefsWithDatabase();
    }

    private static void RegisterDefsWithDatabase() {
        foreach (Type defType in typeof(Def).Subclasses()) {
            GenericHelpers.InvokeStaticMethodOnGenericType(typeof(DefRepository<>), defType, "AddFiltered", Defs);
        }
    }

    private static IEnumerable<XmlAsset> LoadXmlFiles(string dataDirectory) {
        FileInfo[] files = new DirectoryInfo(dataDirectory).GetFiles("*.xml", SearchOption.AllDirectories);
        foreach (FileInfo file in files) {
            yield return new XmlAsset(File.ReadAllText(file.FullName));
        }
    }

    private static XmlDocument MergeFilesIntoSingleDocument(IEnumerable<XmlAsset> list, Dictionary<XmlNode, XmlAsset> assetByNodes) {
        XmlDocument xmlDocument = new();
        xmlDocument.AppendChild(xmlDocument.CreateElement(DataListName));
        foreach (XmlAsset xml in list) {
            foreach (XmlNode? childNode in xml.Document.DocumentElement!.ChildNodes) {
                if (childNode == null) {
                    continue;
                }

                XmlNode node = xmlDocument.ImportNode(childNode, true);
                assetByNodes[node] = xml;
                xmlDocument.DocumentElement!.AppendChild(node);
            }
        }

        return xmlDocument;
    }

    private static void ProcessDocument(XmlDocument root, Dictionary<XmlNode, XmlAsset> assetByNodes) {
        List<XmlNode> rootDefNodes = new();
        foreach (XmlNode? item in root.DocumentElement!.ChildNodes) {
            if (item == null) {
                continue;
            }

            rootDefNodes.Add(item);
            if (item.NodeType == XmlNodeType.Element) {
                //assetByNodes.TryGetValue(item, out XmlAsset? value);
                XmlInheritance.TryRegister(item);
            }
        }

        XmlInheritance.Resolve();

        foreach (XmlNode node in rootDefNodes) {
            Def? def = DeserializeDef(node);
            if (def == null) {
                continue;
            }

            Defs.Add(def);
        }
    }

    private static Def? DeserializeDef(XmlNode node) {
        if (node.NodeType != XmlNodeType.Element) {
            Log.Error($"Failed to deserialize Def: {node.OuterXml}");
            return null;
        }

        XmlAttribute? xmlAttribute = node.Attributes?["Abstract"];
        if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true") {
            return null;
        }

        //todo support assemblies
        Type? type = GenTypes.GetTypeInAnyAssembly(node.Name);
        if (type == null || !typeof(Def).IsAssignableFrom(type)) {
            Log.Error($"Failed to deserialize Def: {node.OuterXml}");
            return null;
        }

        Def? def = (Def?) GenericHelpers.InvokeStaticGenericMethod(typeof(DirectXmlToObject), type, "ObjectFromXmlReflection", node, true);
        return def;
    }
}

public class XmlAsset {
    public XmlDocument Document { get; }
    public XmlReader Reader { get; }

    public XmlAsset(string content) {
        XmlReaderSettings settings = new() {
            IgnoreComments = true, IgnoreWhitespace = true, CheckCharacters = false
        };
        using StringReader input = new(content);
        Reader = XmlReader.Create(input, settings);
        Document = new XmlDocument();
        Document.Load(Reader);
    }
}