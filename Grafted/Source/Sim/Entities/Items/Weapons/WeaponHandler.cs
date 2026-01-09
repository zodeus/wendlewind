using Grafted.Scenes.MainGameScene.Gui;

namespace Grafted.Sim.Entities.Items.Weapons;

/// <summary>
/// Base class for unique weapon handlers that execute special effects during combat.
/// </summary>
public abstract class WeaponHandler : IExposable
{
    public Item Weapon = null!;

    public string Label => Weapon.Label;

    /// <summary>
    /// Called after the weapon successfully hits a target and deals damage.
    /// </summary>
    /// <param name="attacker">The pawn wielding the weapon</param>
    /// <param name="victim">The pawn that was hit</param>
    /// <param name="request">The damage request containing attack details</param>
    /// <param name="damageRecord">The specific damage record for this hit</param>
    public virtual void OnHit(Pawn attacker, Pawn victim, DamageRequest request, DamageRecord damageRecord)
    {
    }

    public virtual void Tick()
    {
    }

    /// <summary>
    /// Creates a custom widget for the weapon's info panel, replacing the default description.
    /// Override this to provide unique weapon-specific UI with custom information, 
    /// interactive controls, or configuration options.
    /// </summary>
    /// <param name="gui">The base GUI context for creating widgets</param>
    /// <returns>A custom widget, or null to use the default description</returns>
    public virtual Widget? CreateInfoWidget(BaseGui gui)
    {
        return null;
    }

    /// <summary>
    /// Called each frame to update the custom info widget created by CreateInfoWidget.
    /// Override this to refresh dynamic content in your custom widget.
    /// </summary>
    /// <param name="widget">The widget previously created by CreateInfoWidget</param>
    public virtual void UpdateInfoWidget(Widget widget)
    {
    }

    public virtual void ExposeData()
    {
        ScribeReferences.Look(ref Weapon!, "Weapon");
    }

    public override string ToString()
    {
        return $"{Weapon.Label} Handler";
    }
}
