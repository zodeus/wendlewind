using Wendlewind.Definitions;
using Wendlewind.Definitions.Loader;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Sim;
using Wendlewind.Sim.Entities.Pawns;
using Wendlewind.Sim.Zones;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

DataLoader.Load();
GameContext.Random = new Random(384710648);

var context = new GameContext();
GameContext.Current = context;
context.Initialize();

int zoneCount = DefRepository<ZoneDef>.Defs.Count;
int pawnCount = DefRepository<PawnDef>.Defs.Count;
Console.WriteLine(
    $"Headless defs loaded. Zones={zoneCount} Pawns={pawnCount} Player={context.PlayerPawn.Label}");

try
{
    var firstZone = DefRepository<ZoneDef>.Defs.OrderBy(z => z.Stage).First();
    context.EnterZone(firstZone);
    context.CurrentZone!.NextEncounter();

    int ticks = 0;
    for (int i = 0; i < 180 && context.CurrentZone.State == ZoneState.Combat; i++)
    {
        context.Tick();
        ticks++;
    }

    Console.WriteLine(
        $"Combat smoke: zone={firstZone.Moniker} state={context.CurrentZone.State} ticks={ticks}");
}
catch (Exception ex)
{
    Console.WriteLine($"Combat smoke skipped: {ex}");
}

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
