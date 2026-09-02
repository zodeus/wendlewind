namespace Wendlemire.Sim.Arena;

public enum PrepSlotKind
{
    Medical,
    Potion,
    Incense,
    Food
}

public readonly record struct PrepSlotCaps(int Medical, int Potion, int Incense, int Food)
{
    public int Of(PrepSlotKind kind)
    {
        return kind switch
        {
            PrepSlotKind.Medical => Medical,
            PrepSlotKind.Potion => Potion,
            PrepSlotKind.Incense => Incense,
            PrepSlotKind.Food => Food,
            _ => 0
        };
    }
}

/// <summary>
/// Prep-screen tile capacity by upcoming arena round. Round 1 is the starting kit;
/// every category is fully open by round 8.
/// </summary>
public static class PrepSlotUnlocks
{
    public const int FullyUnlockedRound = 8;

    private static readonly PrepSlotCaps[] ByRound =
    [
        new(3, 2, 1, 1),
        new(4, 2, 1, 2),
        new(5, 3, 1, 2),
        new(6, 3, 2, 2),
        new(8, 3, 2, 3),
        new(10, 4, 2, 3),
        new(11, 4, 3, 3),
        new(12, 4, 3, 4)
    ];

    public static PrepSlotCaps ForRound(int round)
    {
        var index = Math.Clamp(round, 1, FullyUnlockedRound) - 1;
        return ByRound[index];
    }

    public static int UnlockRound(PrepSlotKind kind, int slotNumber)
    {
        if (slotNumber < 1)
        {
            return 1;
        }

        for (var round = 1; round <= FullyUnlockedRound; round++)
        {
            if (ForRound(round).Of(kind) >= slotNumber)
            {
                return round;
            }
        }

        return FullyUnlockedRound;
    }
}
