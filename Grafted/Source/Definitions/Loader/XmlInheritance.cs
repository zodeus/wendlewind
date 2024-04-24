using System.Xml;
using Grafted.Definitions.Loader.Simulation.Persistence;

namespace Grafted.Definitions.Loader;
#pragma warning disable 8602
#pragma warning disable 8618
public static class XmlInheritance {
    private class XmlInheritanceNode {
        public XmlNode XmlNode;


        public XmlNode ResolvedXmlNode;

        public XmlInheritanceNode Parent;

        public readonly List<XmlInheritanceNode> Children = new();
    }

    private static readonly Dictionary<XmlNode, XmlInheritanceNode> ResolvedNodes;

    private static readonly List<XmlInheritanceNode> UnresolvedNodes;

    private static readonly Dictionary<string, List<XmlInheritanceNode>> NodesByName;

    public static readonly HashSet<string> AllowDuplicateNodesFieldNames;

    private const string NameAttributeName = "Name";

    private const string ParentNameAttributeName = "ParentName";

    private const string InheritAttributeName = "Inherit";

    private static readonly HashSet<string> TempUsedNodeNames;

    static XmlInheritance() {
        ResolvedNodes = new Dictionary<XmlNode, XmlInheritanceNode>();
        UnresolvedNodes = new List<XmlInheritanceNode>();
        NodesByName = new Dictionary<string, List<XmlInheritanceNode>>();
        AllowDuplicateNodesFieldNames = new HashSet<string>();
        TempUsedNodeNames = new HashSet<string>();
    }

    public static void TryRegisterAllFrom(XmlAsset xmlAsset) {
        foreach (XmlNode childNode in xmlAsset.Document.DocumentElement!.ChildNodes) {
            if (childNode.NodeType == XmlNodeType.Element) {
                TryRegister(childNode);
            }
        }
    }

    public static void TryRegister(XmlNode node) {
        XmlAttribute nameAttribute = node.Attributes[NameAttributeName];
        XmlAttribute parentNameAttribute = node.Attributes[ParentNameAttributeName];
        if (nameAttribute == null && parentNameAttribute == null) {
            return;
        }

        List<XmlInheritanceNode>? value = null;
        if (nameAttribute != null && NodesByName.TryGetValue(nameAttribute.Value, out value)) {
            if (value.Count > 0) {
                Log.Error("XML error: Could not register node named \"" + nameAttribute.Value + "\" because this name is already used.");
                return;
            }
        }

        XmlInheritanceNode xmlInheritanceNode = new() { XmlNode = node };
        UnresolvedNodes.Add(xmlInheritanceNode);
        if (nameAttribute != null) {
            if (value != null) {
                value.Add(xmlInheritanceNode);
                return;
            }

            value = new List<XmlInheritanceNode> { xmlInheritanceNode };
            NodesByName.Add(nameAttribute.Value, value);
        }
    }

    public static void Resolve() {
        ResolveParentsAndChildNodesLinks();
        ResolveXmlNodes();
    }

    public static XmlNode GetResolvedNodeFor(XmlNode originalNode) {
        if (originalNode.Attributes[ParentNameAttributeName] != null) {
            if (ResolvedNodes.TryGetValue(originalNode, out XmlInheritanceNode value)) {
                return value.ResolvedXmlNode;
            }

            if (UnresolvedNodes.Any((XmlInheritanceNode x) => x.XmlNode == originalNode)) {
                Log.Error("XML error: XML node \"" + originalNode.Name + "\" has not been resolved yet. There's probably a Resolve() call missing somewhere.");
            }
            else {
                Log.Error("XML error: Tried to get resolved node for node \"" + originalNode.Name +
                          "\" which uses a ParentName attribute, but it is not in a resolved nodes collection, which means that it was never registered or there was an error while resolving it.");
            }
        }

        return originalNode;
    }

    public static void Clear() {
        ResolvedNodes.Clear();
        UnresolvedNodes.Clear();
        NodesByName.Clear();
    }

    private static void ResolveParentsAndChildNodesLinks() {
        for (int i = 0; i < UnresolvedNodes.Count; i++) {
            XmlAttribute xmlAttribute = UnresolvedNodes[i].XmlNode.Attributes["ParentName"];
            if (xmlAttribute != null) {
                UnresolvedNodes[i].Parent = GetBestParentFor(UnresolvedNodes[i], xmlAttribute.Value);
                if (UnresolvedNodes[i].Parent != null) {
                    UnresolvedNodes[i].Parent.Children.Add(UnresolvedNodes[i]);
                }
            }
        }
    }

    private static void ResolveXmlNodes() {
        List<XmlInheritanceNode> list = UnresolvedNodes.Where((XmlInheritanceNode x) => x.Parent == null || x.Parent.ResolvedXmlNode != null).ToList();
        for (int i = 0; i < list.Count; i++) {
            ResolveXmlNodesRecursively(list[i]);
        }

        for (int j = 0; j < UnresolvedNodes.Count; j++) {
            if (UnresolvedNodes[j].ResolvedXmlNode == null) {
                Log.Error("XML error: Cyclic inheritance hierarchy detected for node \"" + UnresolvedNodes[j].XmlNode.Name + "\". Full node: " + UnresolvedNodes[j].XmlNode.OuterXml);
            }
            else {
                ResolvedNodes.Add(UnresolvedNodes[j].XmlNode, UnresolvedNodes[j]);
            }
        }

        UnresolvedNodes.Clear();
    }

    private static void ResolveXmlNodesRecursively(XmlInheritanceNode node) {
        if (node.ResolvedXmlNode != null) {
            Log.Error("XML error: Cyclic inheritance hierarchy detected for node \"" + node.XmlNode.Name + "\". Full node: " + node.XmlNode.OuterXml);
            return;
        }

        ResolveXmlNodeFor(node);
        for (int i = 0; i < node.Children.Count; i++) {
            ResolveXmlNodesRecursively(node.Children[i]);
        }
    }

    private static XmlInheritanceNode? GetBestParentFor(XmlInheritanceNode node, string parentName) {
        XmlInheritanceNode xmlInheritanceNode = null;
        if (NodesByName.TryGetValue(parentName, out List<XmlInheritanceNode> value)) {
            if (value.Count > 0) {
                xmlInheritanceNode = value[0];
            }
        }

        if (xmlInheritanceNode == null) {
            Log.Error("XML error: Could not find parent node named \"" + parentName + "\" for node \"" + node.XmlNode.Name + "\". Full node: " + node.XmlNode.OuterXml);
            return null;
        }

        return xmlInheritanceNode;
    }

    private static void ResolveXmlNodeFor(XmlInheritanceNode node) {
        if (node.Parent == null) {
            node.ResolvedXmlNode = node.XmlNode;
            return;
        }

        if (node.Parent.ResolvedXmlNode == null) {
            Log.Error("XML error: Internal error. Tried to resolve node whose parent has not been resolved yet. This means that this method was called in incorrect order.");
            node.ResolvedXmlNode = node.XmlNode;
            return;
        }

        CheckForDuplicateNodes(node.XmlNode, node.XmlNode);
        XmlNode xmlNode = node.Parent.ResolvedXmlNode.CloneNode(deep: true);
        RecursiveNodeCopyOverwriteElements(node.XmlNode, xmlNode);
        node.ResolvedXmlNode = xmlNode;
    }

    private static void RecursiveNodeCopyOverwriteElements(XmlNode child, XmlNode current) {
        XmlAttribute xmlAttribute = child.Attributes[InheritAttributeName];
        if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "false") {
            while (current.HasChildNodes) {
                current.RemoveChild(current.FirstChild);
            }

            foreach (XmlNode item in child) {
                XmlNode newChild = current.OwnerDocument.ImportNode(item, deep: true);
                current.AppendChild(newChild);
            }
        }
        else {
            current.Attributes.RemoveAll();
            XmlAttributeCollection attributes = child.Attributes;
            for (int i = 0; i < attributes.Count; i++) {
                XmlAttribute node2 = (XmlAttribute) current.OwnerDocument.ImportNode(attributes[i], deep: true);
                current.Attributes.Append(node2);
            }

            List<XmlElement> list = new();
            XmlNode xmlNode = null;
            foreach (XmlNode item2 in child) {
                if (item2.NodeType == XmlNodeType.Text) {
                    xmlNode = item2;
                }
                else if (item2.NodeType == XmlNodeType.Element) {
                    list.Add((XmlElement) item2);
                }
            }

            if (xmlNode != null) {
                for (int num = current.ChildNodes.Count - 1; num >= 0; num--) {
                    XmlNode xmlNode3 = current.ChildNodes[num];
                    if (xmlNode3.NodeType != XmlNodeType.Attribute) {
                        current.RemoveChild(xmlNode3);
                    }
                }

                XmlNode newChild2 = current.OwnerDocument.ImportNode(xmlNode, deep: true);
                current.AppendChild(newChild2);
            }
            else if (!list.Any()) {
                bool flag = false;
                foreach (XmlNode childNode in current.ChildNodes) {
                    if (childNode.NodeType == XmlNodeType.Element) {
                        flag = true;
                        break;
                    }
                }

                if (!flag) {
                    foreach (XmlNode childNode2 in current.ChildNodes) {
                        if (childNode2.NodeType != XmlNodeType.Attribute) {
                            current.RemoveChild(childNode2);
                        }
                    }
                }
            }
            else {
                for (int j = 0; j < list.Count; j++) {
                    XmlElement xmlElement = list[j];
                    if (IsListElement(xmlElement)) {
                        XmlNode newChild3 = current.OwnerDocument.ImportNode(xmlElement, deep: true);
                        current.AppendChild(newChild3);
                    }
                    else {
                        XmlElement xmlElement2 = current[xmlElement.Name];
                        if (xmlElement2 != null) {
                            RecursiveNodeCopyOverwriteElements(xmlElement, xmlElement2);
                        }
                        else {
                            XmlNode newChild4 = current.OwnerDocument.ImportNode(xmlElement, deep: true);
                            current.AppendChild(newChild4);
                        }
                    }
                }
            }
        }
    }

    private static void CheckForDuplicateNodes(XmlNode node, XmlNode root) {
        TempUsedNodeNames.Clear();
        foreach (XmlNode childNode in node.ChildNodes) {
            if (childNode.NodeType == XmlNodeType.Element && !IsListElement(childNode)) {
                if (TempUsedNodeNames.Contains(childNode.Name)) {
                    Log.Error("XML error: Duplicate XML node name " + childNode.Name + " in this XML block: " + node.OuterXml + ((node != root) ? ("\n\nRoot node: " + root.OuterXml) : ""));
                }
                else {
                    TempUsedNodeNames.Add(childNode.Name);
                }
            }
        }

        TempUsedNodeNames.Clear();
        foreach (XmlNode childNode2 in node.ChildNodes) {
            if (childNode2.NodeType == XmlNodeType.Element) {
                CheckForDuplicateNodes(childNode2, root);
            }
        }
    }

    private static bool IsListElement(XmlNode node) {
        if (node.Name != DirectXmlToObject.ListItemNodeName) {
            if (node.ParentNode != null) {
                return AllowDuplicateNodesFieldNames.Contains(node.ParentNode.Name);
            }

            return false;
        }

        return true;
    }
}