using Wendlewind.Definitions;
using Wendlewind.Definitions.Loader;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim;
using Wendlewind.Sim.Combat;
using Wendlewind.Sim.Entities.Pawns;
using Wendlewind.Sim.Zones;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

DataLoader.Load();

var replay = CombatReplay.AssertDeterministic();
Console.WriteLine(
    $"Replay agreed. Zone={replay.ZoneMoniker} Enemy={replay.EnemyMoniker} " +
    $"Ticks={replay.Ticks} PlayerAlive={replay.PlayerAlive} Cause={replay.CauseOfDeath ?? "-"}");

var context = GameContext.Current;
int zoneCount = DefRepository<ZoneDef>.Defs.Count;
int pawnCount = DefRepository<PawnDef>.Defs.Count;

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    zones = zoneCount,
    pawns = pawnCount,
    player = context.PlayerPawn.Label
}));

app.MapPost("/builds", (BuildSnapshot snapshot) =>
{
    return Results.Accepted($"/builds/{snapshot.BuildId}", snapshot);
});

app.MapGet("/opponent", () =>
{
    return Results.Ok(new BuildSnapshot
    {
        PlayerId = "bot",
        BuildId = "placeholder",
        EntityDefMonikers = Array.Empty<string>(),
        Seed = 0
    });
});

app.MapPost("/matches", (MatchRequest request) =>
{
    return Results.Ok(new CombatResult
    {
        MatchId = Guid.NewGuid().ToString("N"),
        WinnerPlayerId = request.Attacker.PlayerId,
        Ticks = 0,
        CauseOfDeath = "stub"
    });
});

app.Run();
