using System.Xml;

namespace Grafted.Definitions.Loader;
#pragma warning disable 8602
#pragma warning disable 8618
public static class XmlInheritance
{
    private class XmlInheritanceNode
    {
        public XmlNode XmlNode;
        public XmlNode? ResolvedXmlNode;
        public XmlInheritanceNode? Parent;
        public readonly List<XmlInheritanceNode> Children = new();
    }

    private static readonly Dictionary<XmlNode, XmlInheritanceNode> ResolvedNodes;
    private static readonly List<XmlInheritanceNode> UnresolvedNodes;
    private static readonly Dictionary<string, List<XmlInheritanceNode>> NodesByName;
    private const string NameAttributeName = "Name";
    private const string ParentNameAttributeName = "ParentName";
    private const string InheritAttributeName = "Inherit";
    private static readonly HashSet<string> TempUsedNodeNames;

    public static readonly HashSet<string> AllowDuplicateNodesFieldNames;

    static XmlInheritance()
    {
        ResolvedNodes = new Dictionary<XmlNode, XmlInheritanceNode>();
        UnresolvedNodes = new List<XmlInheritanceNode>();
        NodesByName = new Dictionary<string, List<XmlInheritanceNode>>();
        AllowDuplicateNodesFieldNames = new HashSet<string>();
        TempUsedNodeNames = new HashSet<string>();
    }

    public static void TryRegisterAllFrom(XmlAsset xmlAsset)
    {
        foreach (XmlNode childNode in xmlAsset.Document.DocumentElement!.ChildNodes)
        {
            if (childNode.NodeType == XmlNodeType.Element)
            {
                TryRegister(childNode);
            }
        }
    }

    public static void TryRegister(XmlNode node)
    {
        var nameAttribute = node.Attributes[NameAttributeName];
        var parentNameAttribute = node.Attributes[ParentNameAttributeName];
        if (nameAttribute == null && parentNameAttribute == null)
        {
            return;
        }

        List<XmlInheritanceNode>? value = null;
        if (nameAttribute != null && NodesByName.TryGetValue(nameAttribute.Value, out value))
        {
            if (value.Count > 0)
            {
                Log.Error("XML error: Could not register node named \"" + nameAttribute.Value + "\" because this name is already used.");
                return;
            }
        }

        var xmlInheritanceNode = new XmlInheritanceNode { XmlNode = node };
        UnresolvedNodes.Add(xmlInheritanceNode);
        if (nameAttribute != null)
        {
            if (value != null)
            {
                value.Add(xmlInheritanceNode);
                return;
            }

            value = [xmlInheritanceNode];
            NodesByName.Add(nameAttribute.Value, value);
        }
    }

    public static void Resolve()
    {
        ResolveParentsAndChildNodesLinks();
        ResolveXmlNodes();
    }

    public static XmlNode? GetResolvedNodeFor(XmlNode originalNode)
    {
        if (originalNode.Attributes[ParentNameAttributeName] != null)
        {
            if (ResolvedNodes.TryGetValue(originalNode, out var value))
            {
                return value.ResolvedXmlNode;
            }

            if (UnresolvedNodes.Any(x => x.XmlNode == originalNode))
            {
                Log.Error("XML error: XML node \"" + originalNode.Name + "\" has not been resolved yet. There's probably a Resolve() call missing somewhere.");
            }
            else
            {
                Log.Error("XML error: Tried to get resolved node for node \"" + originalNode.Name +
                          "\" which uses a ParentName attribute, but it is not in a resolved nodes collection, which means that it was never registered or there was an error while resolving it.");
            }
        }

        return originalNode;
    }

    public static void Clear()
    {
        ResolvedNodes.Clear();
        UnresolvedNodes.Clear();
        NodesByName.Clear();
    }

    private static void ResolveParentsAndChildNodesLinks()
    {
        for (var i = 0; i < UnresolvedNodes.Count; i++)
        {
            var xmlAttribute = UnresolvedNodes[i].XmlNode.Attributes["ParentName"];
            if (xmlAttribute != null)
            {
                UnresolvedNodes[i].Parent = GetBestParentFor(UnresolvedNodes[i], xmlAttribute.Value);
                if (UnresolvedNodes[i].Parent != null)
                {
                    UnresolvedNodes[i].Parent.Children.Add(UnresolvedNodes[i]);
                }
            }
        }
    }

    private static void ResolveXmlNodes()
    {
        var list = UnresolvedNodes.Where(x => x.Parent == null || x.Parent.ResolvedXmlNode != null).ToList();
        for (var i = 0; i < list.Count; i++)
        {
            ResolveXmlNodesRecursively(list[i]);
        }

        for (var j = 0; j < UnresolvedNodes.Count; j++)
        {
            if (UnresolvedNodes[j].ResolvedXmlNode == null)
            {
                Log.Error("XML error: Cyclic inheritance hierarchy detected for node \"" + UnresolvedNodes[j].XmlNode.Name + "\". Full node: " + UnresolvedNodes[j].XmlNode.OuterXml);
            }
            else
            {
                ResolvedNodes.Add(UnresolvedNodes[j].XmlNode, UnresolvedNodes[j]);
            }
        }

        UnresolvedNodes.Clear();
    }

    private static void ResolveXmlNodesRecursively(XmlInheritanceNode node)
    {
        if (node.ResolvedXmlNode != null)
        {
            Log.Error("XML error: Cyclic inheritance hierarchy detected for node \"" + node.XmlNode.Name + "\". Full node: " + node.XmlNode.OuterXml);
            return;
        }

        ResolveXmlNodeFor(node);
        foreach (var t in node.Children)
        {
            ResolveXmlNodesRecursively(t);
        }
    }

    private static XmlInheritanceNode? GetBestParentFor(XmlInheritanceNode node, string parentName)
    {
        XmlInheritanceNode? xmlInheritanceNode = null;
        if (NodesByName.TryGetValue(parentName, out var value))
        {
            if (value.Count > 0)
            {
                xmlInheritanceNode = value[0];
            }
        }

        if (xmlInheritanceNode == null)
        {
            Log.Error("XML error: Could not find parent node named \"" + parentName + "\" for node \"" + node.XmlNode.Name + "\". Full node: " + node.XmlNode.OuterXml);
            return null;
        }

        return xmlInheritanceNode;
    }

    private static void ResolveXmlNodeFor(XmlInheritanceNode node)
    {
        if (node.Parent == null)
        {
            node.ResolvedXmlNode = node.XmlNode;
            return;
        }

        if (node.Parent.ResolvedXmlNode == null)
        {
            Log.Error("XML error: Internal error. Tried to resolve node whose parent has not been resolved yet. This means that this method was called in incorrect order.");
            node.ResolvedXmlNode = node.XmlNode;
            return;
        }

        CheckForDuplicateNodes(node.XmlNode, node.XmlNode);
        var xmlNode = node.Parent.ResolvedXmlNode.CloneNode(deep: true);
        RecursiveNodeCopyOverwriteElements(node.XmlNode, xmlNode);
        node.ResolvedXmlNode = xmlNode;
    }

    private static void RecursiveNodeCopyOverwriteElements(XmlNode child, XmlNode current)
    {
        var xmlAttribute = child.Attributes[InheritAttributeName];
        if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "false")
        {
            while (current.HasChildNodes)
            {
                current.RemoveChild(current.FirstChild!);
            }

            foreach (XmlNode item in child)
            {
                var newChild = current.OwnerDocument.ImportNode(item, deep: true);
                current.AppendChild(newChild);
            }
        }
        else
        {
            current.Attributes.RemoveAll();
            var attributes = child.Attributes;
            for (var i = 0; i < attributes.Count; i++)
            {
                var node2 = (XmlAttribute)current.OwnerDocument.ImportNode(attributes[i], deep: true);
                current.Attributes.Append(node2);
            }

            var list = new List<XmlElement>();
            XmlNode? xmlNode = null;
            foreach (XmlNode item2 in child)
            {
                if (item2.NodeType == XmlNodeType.Text)
                {
                    xmlNode = item2;
                }
                else if (item2.NodeType == XmlNodeType.Element)
                {
                    list.Add((XmlElement)item2);
                }
            }

            if (xmlNode != null)
            {
                for (var num = current.ChildNodes.Count - 1; num >= 0; num--)
                {
                    var xmlNode3 = current.ChildNodes[num];
                    if (xmlNode3.NodeType != XmlNodeType.Attribute)
                    {
                        current.RemoveChild(xmlNode3);
                    }
                }

                var newChild2 = current.OwnerDocument.ImportNode(xmlNode, deep: true);
                current.AppendChild(newChild2);
            }
            else if (!list.Any())
            {
                var flag = false;
                foreach (XmlNode childNode in current.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Element)
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    foreach (XmlNode childNode2 in current.ChildNodes)
                    {
                        if (childNode2.NodeType != XmlNodeType.Attribute)
                        {
                            current.RemoveChild(childNode2);
                        }
                    }
                }
            }
            else
            {
                for (var j = 0; j < list.Count; j++)
                {
                    var xmlElement = list[j];
                    if (IsListElement(xmlElement))
                    {
                        var newChild3 = current.OwnerDocument.ImportNode(xmlElement, deep: true);
                        current.AppendChild(newChild3);
                    }
                    else
                    {
                        var xmlElement2 = current[xmlElement.Name];
                        if (xmlElement2 != null)
                        {
                            RecursiveNodeCopyOverwriteElements(xmlElement, xmlElement2);
                        }
                        else
                        {
                            var newChild4 = current.OwnerDocument.ImportNode(xmlElement, deep: true);
                            current.AppendChild(newChild4);
                        }
                    }
                }
            }
        }
    }

    private static void CheckForDuplicateNodes(XmlNode node, XmlNode root)
    {
        TempUsedNodeNames.Clear();
        foreach (XmlNode childNode in node.ChildNodes)
        {
            if (childNode.NodeType == XmlNodeType.Element && !IsListElement(childNode))
            {
                if (TempUsedNodeNames.Contains(childNode.Name))
                {
                    Log.Error("XML error: Duplicate XML node name " + childNode.Name + " in this XML block: " + node.OuterXml + ((node != root) ? ("\n\nRoot node: " + root.OuterXml) : ""));
                }
                else
                {
                    TempUsedNodeNames.Add(childNode.Name);
                }
            }
        }

        TempUsedNodeNames.Clear();
        foreach (XmlNode childNode2 in node.ChildNodes)
        {
            if (childNode2.NodeType == XmlNodeType.Element)
            {
                CheckForDuplicateNodes(childNode2, root);
            }
        }
    }

    private static bool IsListElement(XmlNode node)
    {
        if (node.Name != DirectXmlToObject.ListItemNodeName)
        {
            if (node.ParentNode != null)
            {
                return AllowDuplicateNodesFieldNames.Contains(node.ParentNode.Name);
            }

            return false;
        }

        return true;
    }
}