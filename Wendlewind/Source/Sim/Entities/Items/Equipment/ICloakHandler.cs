namespace Wendlewind.Sim.Entities.Items.Equipment;

/// <summary>
/// Interface for cloak handlers that provide bonus effects.
/// Used by CloakPanel to display bonus information generically.
/// </summary>
public interface ICloakHandler
{
    /// <summary>
    /// Gets the text to display for the current bonus.
    /// </summary>
    string GetBonusDisplayText();
}
