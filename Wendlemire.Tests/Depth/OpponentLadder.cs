using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Tests.Depth;

internal static class OpponentLadder
{
    public static BuildStage StageFor(int round) => round switch
    {
        <= 3 => BuildStage.Early,
        <= 6 => BuildStage.Mid,
        <= 9 => BuildStage.Late,
        _ => BuildStage.End
    };

    public static BuildSnapshot For(int runSeed, int round)
    {
        var stage = StageFor(round);
        var rng = new Random(unchecked(runSeed * 397 ^ round * 7919 ^ 0x5F3759DF));
        var archetypes = (BuildGenerator.Archetype[])Enum.GetValues(typeof(BuildGenerator.Archetype));
        var archetype = archetypes[rng.Next(archetypes.Length)];
        return Tag(BuildGenerator.Generate(stage, archetype, round, rng), $"opp-r{round}");
    }

    public static BuildSnapshot Generate(
        BuildStage stage,
        BuildGenerator.Archetype archetype,
        int index,
        int seed)
    {
        var rng = new Random(unchecked(seed * 397 ^ (int)stage * 7919 ^ (int)archetype * 104729 ^ index));
        return Tag(BuildGenerator.Generate(stage, archetype, index, rng), $"{archetype}-{stage}-{index}");
    }

    public static BuildSnapshot Tag(BuildSnapshot snapshot, string playerId) =>
        snapshot with { PlayerId = playerId };
}
