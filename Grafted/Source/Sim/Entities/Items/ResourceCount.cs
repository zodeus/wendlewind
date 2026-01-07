using System.Xml;
using Grafted.Definitions.Loader;

namespace Grafted.Sim.Entities.Items;

[UsedImplicitly]
public class ResourceCount {
    public ItemDef Item = null!;
    public int Count;
    
    public ResourceCount() { }
    
    public ResourceCount(Item item, int count) {
        Item = item.ItemDef;
        Count = count;
    }

    public ResourceCount(ItemDef def, int count) {
        Item = def;
        Count = count;
    }
    
    /// <summary>
    /// Custom XML parser that supports both compact and verbose syntax:
    /// <para>Compact: <code>&lt;ItemMoniker&gt;Count&lt;/ItemMoniker&gt;</code></para>
    /// <para>Verbose: <code>&lt;li&gt;&lt;Item&gt;ItemMoniker&lt;/Item&gt;&lt;Count&gt;N&lt;/Count&gt;&lt;/li&gt;</code></para>
    /// </summary>
    [UsedImplicitly]
    public void LoadDataFromXmlCustom(XmlNode xmlRoot) {
        // Check if this is the verbose format with <Item> and <Count> child nodes
        var itemNode = xmlRoot["Item"];
        var countNode = xmlRoot["Count"];
        
        if (itemNode != null && countNode != null)
        {
            // Verbose format: <li><Item>ItemMoniker</Item><Count>N</Count></li>
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "Item", itemNode.InnerText);
            Count = ParseHelper.FromString<int>(countNode.InnerText);
        }
        else
        {
            // Compact format: <ItemMoniker>Count</ItemMoniker>
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "Item", xmlRoot.Name);
            Count = ParseHelper.FromString<int>(xmlRoot.FirstChild!.Value!);
        }
    }
}