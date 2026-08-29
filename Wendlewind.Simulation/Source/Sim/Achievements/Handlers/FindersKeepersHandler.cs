namespace Wendlewind.Sim.Achievements.Handlers;

using Wendlewind.Sim.Entities;
/// <summary>
/// Unlocks when the player finds an item (looting from combat)
/// Benefit: Start with the Grimoire trinket
/// </summary>
public class FindersKeepersHandler : AchievementHandler
{
    public FindersKeepersHandler(IRng rng)
    {
        Rng = rng;
    }

    public override void OnItemFound(Item item)
    {
        if (IsUnlocked) return;

        if (item.Def == Defs.Items.Grimoire)
        {
            Unlock();
            return;
        }
    }
}

