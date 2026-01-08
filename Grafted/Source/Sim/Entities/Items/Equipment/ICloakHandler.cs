namespace Grafted.Sim.Entities.Items.Equipment;

/// <summary>
/// Interface for cloak handlers that provide bonus effects.
/// Used by CloakPanel to display bonus information generically.
/// </summary>
public interface ICloakHandler
{
    /// <summary>
    /// The color used to display the bonus text.
    /// </summary>
    Color BonusColor { get; }
    
    /// <summary>
    /// The label for the type of bonus (e.g., "Healing", "Strength").
    /// </summary>
    string BonusLabel { get; }
    
    /// <summary>
    /// Gets the text to display for the current bonus.
    /// </summary>
    string GetBonusDisplayText();
}
