using System.Xml;
using Wendlemire.Definitions.Loader;

namespace Wendlemire.Sim.Entities.Items;

/// <summary>
/// Defines upgrade data for a single upgrade level.
/// </summary>
[UsedImplicitly]
public class UpgradeData
{
    /// <summary>
    /// The upgrade level this data represents (1, 2, 3, etc.)
    /// </summary>
    public int Level;
    
    /// <summary>
    /// Resource costs required to reach this upgrade level.
    /// </summary>
    public List<ResourceCount> ResourceCosts = [];
    
    /// <summary>
    /// Trinkets that must be equipped/owned to perform this upgrade.
    /// </summary>
    public List<ItemDef> RequiredTrinkets = [];
    
    /// <summary>
    /// Optional description of what this upgrade provides (e.g. "+50% Damage")
    /// </summary>
    public string BonusDescription = "";
    
    /// <summary>
    /// Custom XML parser that allows for compact syntax:
    /// <code>
    /// &lt;Upgrade Level="1" Bonus="+100% Healing"&gt;
    ///     &lt;RequiredTrinkets&gt;TinkersToolbox&lt;/RequiredTrinkets&gt;
    ///     &lt;ElvishLeaf&gt;1&lt;/ElvishLeaf&gt;
    ///     &lt;GoldenBean&gt;2&lt;/GoldenBean&gt;
    /// &lt;/Upgrade&gt;
    /// </code>
    /// </summary>
    [UsedImplicitly]
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        // Parse Level attribute
        var levelAttr = xmlRoot.Attributes?["Level"];
        if (levelAttr != null)
        {
            Level = ParseHelper.FromString<int>(levelAttr.Value);
        }
        
        // Parse Bonus attribute
        var bonusAttr = xmlRoot.Attributes?["Bonus"];
        if (bonusAttr != null)
        {
            BonusDescription = bonusAttr.Value;
        }
        
        // Parse child nodes
        foreach (XmlNode childNode in xmlRoot.ChildNodes)
        {
            if (childNode is XmlComment) continue;
            
            if (childNode.Name == "RequiredTrinkets")
            {
                // Parse comma or space separated trinket names
                var trinketNames = childNode.InnerText.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
                foreach (var trinketName in trinketNames)
                {
                    DirectXmlCrossRefLoader.RegisterListWantsCrossRef(RequiredTrinkets, trinketName.Trim(), "RequiredTrinkets");
                }
            }
            else
            {
                // Treat as ResourceCount: <ItemMoniker>Count</ItemMoniker>
                var resourceCount = new ResourceCount();
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(resourceCount, "Item", childNode.Name);
                resourceCount.Count = ParseHelper.FromString<int>(childNode.FirstChild!.Value!);
                ResourceCosts.Add(resourceCount);
            }
        }
    }
}

/// <summary>
/// Properties defining the upgrade path for an item.
/// </summary>
[UsedImplicitly]
public class UpgradeProperties
{
    /// <summary>
    /// List of available upgrades, ordered by level.
    /// </summary>
    public List<UpgradeData> Upgrades = [];
    
    /// <summary>
    /// Gets the upgrade data for the next level after the current one.
    /// </summary>
    public UpgradeData? GetNextUpgrade(int currentLevel)
    {
        return Upgrades.FirstOrDefault(u => u.Level == currentLevel + 1);
    }
    
    /// <summary>
    /// Gets the upgrade data for a specific level.
    /// </summary>
    public UpgradeData? GetUpgrade(int level)
    {
        return Upgrades.FirstOrDefault(u => u.Level == level);
    }
    
    /// <summary>
    /// Gets the maximum upgrade level available.
    /// </summary>
    public int MaxLevel => Upgrades.Count > 0 ? Upgrades.Max(u => u.Level) : 0;
}

/// <summary>
/// Interface for handlers that support the upgrade system.
/// </summary>
public interface IUpgradableHandler
{
    /// <summary>
    /// The current upgrade level.
    /// </summary>
    int UpgradeLevel { get; }
    
    /// <summary>
    /// Gets the upgrade properties from the item definition.
    /// </summary>
    UpgradeProperties? UpgradeProperties { get; }
    
    /// <summary>
    /// Sets the upgrade level. Used by the default TryUpgrade implementation.
    /// </summary>
    protected void SetUpgradeLevel(int level);
    
    /// <summary>
    /// Checks if the item can be upgraded with the given inventory.
    /// </summary>
    bool CanUpgrade(PawnInventory inventory)
    {
        var nextUpgrade = UpgradeProperties?.GetNextUpgrade(UpgradeLevel);
        if (nextUpgrade == null) return false;

        // Check required trinkets
        foreach (var trinketDef in nextUpgrade.RequiredTrinkets)
        {
            if (!inventory.Trinkets.Any(t => t.Def == trinketDef))
                return false;
        }

        // Check resource costs
        foreach (var cost in nextUpgrade.ResourceCosts)
        {
            if (inventory.AmountOf(cost.Item) < cost.Count)
                return false;
        }

        return true;
    }
    
    /// <summary>
    /// Attempts to upgrade the item using resources from inventory.
    /// </summary>
    bool TryUpgrade(PawnInventory inventory)
    {
        var nextUpgrade = UpgradeProperties?.GetNextUpgrade(UpgradeLevel);
        if (nextUpgrade == null || !CanUpgrade(inventory)) return false;

        // Deduct resources
        List<Item> takenResources = [];
        foreach (var cost in nextUpgrade.ResourceCosts)
        {
            var taken = inventory.Take(cost);
            if (taken == null)
            {
                // Rollback if something fails
                foreach (var item in takenResources)
                    inventory.TryAdd(item);
                return false;
            }
            takenResources.Add(taken);
        }

        // Destroy taken resources
        foreach (var item in takenResources)
            item.Destroy();

        SetUpgradeLevel(nextUpgrade.Level);
        return true;
    }
    
    /// <summary>
    /// Gets the bonus description for the current upgrade level.
    /// </summary>
    string GetCurrentBonusDescription()
    {
        if (UpgradeLevel == 0) return "";
        var upgrade = UpgradeProperties?.GetUpgrade(UpgradeLevel);
        return upgrade?.BonusDescription ?? "";
    }
}
