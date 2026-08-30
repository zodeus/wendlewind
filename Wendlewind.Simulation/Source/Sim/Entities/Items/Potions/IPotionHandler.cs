namespace Wendlewind.Sim.Entities.Items.Potions;

/// <summary>
/// Interface for potion handlers that define potion-specific behavior.
/// </summary>
public interface IPotionHandler
{
    /// <summary>
    /// The potion item this handler is attached to.
    /// </summary>
    Item Potion { get; set; }
    
    /// <summary>
    /// Whether this potion can be used during combat.
    /// </summary>
    bool CanUseInCombat { get; }
    
    /// <summary>
    /// Whether this potion can be used outside of combat.
    /// </summary>
    bool CanUseOutsideCombat { get; }
    
    /// <summary>
    /// Who receives the potion's combat effect (user for sips, opponent for thrown flasks).
    /// </summary>
    Pawn GetCombatApplicationTarget(Pawn user, Pawn? opponent);

    /// <summary>
    /// Use the potion on a target pawn during combat.
    /// </summary>
    /// <param name="user">The pawn using the potion</param>
    /// <param name="target">The target pawn (could be same as user or opponent)</param>
    /// <returns>Result describing what happened</returns>
    PotionUseResult UseInCombat(Pawn user, Pawn? target = null);
    
    /// <summary>
    /// Use the potion on a target pawn outside of combat.
    /// </summary>
    /// <param name="user">The pawn using the potion</param>
    /// <returns>Result describing what happened</returns>
    PotionUseResult UseOutsideCombat(Pawn user);
    
    /// <summary>
    /// Get a description of what this potion does.
    /// </summary>
    string GetEffectDescription();
}

/// <summary>
/// Result of using a potion.
/// </summary>
public class PotionUseResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string? AlertMessage { get; init; }
    public Color AlertColor { get; init; } = Color.White;
    
    public static PotionUseResult Succeeded(string message, string? alertMessage = null, Color? alertColor = null) => new()
    {
        Success = true,
        Message = message,
        AlertMessage = alertMessage,
        AlertColor = alertColor ?? Color.GreenYellow
    };
    
    public static PotionUseResult Failed(string message) => new()
    {
        Success = false,
        Message = message
    };
}
