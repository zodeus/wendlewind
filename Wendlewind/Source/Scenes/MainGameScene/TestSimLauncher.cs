using Wendlewind.Definitions;
using Wendlewind.NetCode;
using Wendlewind.Sim.Entities.Pawns;

namespace Wendlewind.Scenes.MainGameScene;

public static class TestSimLauncher
{
    public static void StartEncounter(GameContext context)
    {
        ResetToZone(context);

        var attacker = context.PlayerPawn;
        var defender = CreateHumanOpponent(context);

        var attackerBuild = TestSimSettings.AttackerOverride ?? BuildTemplates.Get(TestSimSettings.AttackerBuildId);
        BuildSnapshotFactory.Apply(attacker, attackerBuild);
        BuildSnapshotFactory.Apply(defender, BuildTemplates.Get(TestSimSettings.DefenderBuildId));

        context.CurrentZone!.StartHumanDuel(attacker, defender, TestSimSettings.Seed);
    }

    public static void Rematch(GameContext context)
    {
        StartEncounter(context);
    }

    public static void Reroll(GameContext context)
    {
        TestSimSettings.Seed++;
        StartEncounter(context);
    }

    public static void ReturnToSelector(GameContext context)
    {
        ResetToZone(context);
    }

    private static void ResetToZone(GameContext context)
    {
        context.Initialize(TestSimSettings.Seed);
        var zone = context.World.Zones.OrderBy(z => z.ZoneDef.Stage).First();
        context.EnterZone(zone.ZoneDef);
    }

    private static Pawn CreateHumanOpponent(GameContext context)
    {
        var emptyLoadout = DefRepository<PawnLoadoutDef>.GetByMoniker("EmptyLoadout")
                           ?? Defs.PawnLoadouts.DefaultStarterLoadout;

        return PawnGenerator.CreatePawn(
            context,
            new PawnRequest(
                "Chuggins",
                DefRepository<PawnDef>.GetByMoniker("HumanA")!,
                emptyLoadout,
                PawnType.Enemy));
    }
}
