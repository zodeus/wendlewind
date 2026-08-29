using Microsoft.Extensions.DependencyInjection;
using Wendlewind.Definitions;
using Wendlewind.Definitions.Loader;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim;
using Wendlewind.Sim.Combat;
using Wendlewind.Sim.Entities.Pawns;
using Wendlewind.Sim.Zones;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWendlewindSimulation();
var app = builder.Build();

DataLoader.Load();

var replay = CombatReplay.AssertDeterministic();
Console.WriteLine(
    $"Replay agreed. Zone={replay.ZoneMoniker} Enemy={replay.EnemyMoniker} " +
    $"Ticks={replay.Ticks} PlayerAlive={replay.PlayerAlive} Cause={replay.CauseOfDeath ?? "-"}");

using var matchA = app.Services.CreateScope();
using var matchB = app.Services.CreateScope();
var contextA = matchA.ServiceProvider.GetRequiredService<GameContext>();
var contextB = matchB.ServiceProvider.GetRequiredService<GameContext>();
contextA.Initialize(111);
contextB.Initialize(222);
if (ReferenceEquals(contextA.Rng, contextB.Rng) || contextA.RunSeed == contextB.RunSeed)
{
    throw new InvalidOperationException("Scoped matches must own independent seeds and RNG instances.");
}

Console.WriteLine($"Two match scopes isolated. A.RunSeed={contextA.RunSeed} B.RunSeed={contextB.RunSeed}");

int zoneCount = DefRepository<ZoneDef>.Defs.Count;
int pawnCount = DefRepository<PawnDef>.Defs.Count;

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    zones = zoneCount,
    pawns = pawnCount,
    player = contextA.PlayerPawn.Label
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
    using var scope = app.Services.CreateScope();
    var match = scope.ServiceProvider.GetRequiredService<GameContext>();
    match.Initialize(request.Attacker.Seed == 0 ? null : request.Attacker.Seed);
    return Results.Ok(new CombatResult
    {
        MatchId = Guid.NewGuid().ToString("N"),
        WinnerPlayerId = request.Attacker.PlayerId,
        Ticks = 0,
        CauseOfDeath = "stub"
    });
});

app.Run();
