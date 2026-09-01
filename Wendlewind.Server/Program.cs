using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wendlewind.Definitions;
using Wendlewind.Definitions.Loader;
using Wendlewind.NetCode;
using Wendlewind.NetCode.Contracts;
using Wendlewind.Server;
using Wendlewind.Sim;
using Wendlewind.Sim.Arena;
using Wendlewind.Sim.Combat;
using Wendlewind.Sim.Entities.Pawns;
using Wendlewind.Sim.Zones;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWendlewindSimulation();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, NetCodeJsonContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});
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
var dataDir = ServerData.EnsureDirectory();
var pool = new BuildPool(ServerData.PoolPath(dataDir));
var players = new PlayerStore(dataDir);
var analytics = new FightAnalyticsService(players);
var codes = new ActivationCodeStore(dataDir);
var releases = new ReleaseDownloadService(new HttpClient());
var adminAuth = AdminAuth.Create(app.Environment);
Console.WriteLine($"Data directory: {dataDir}");
Console.WriteLine($"Admin: {adminAuth.StatusMessage}");

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapAdmin(adminAuth, players, pool, analytics, codes);

app.MapGet("/downloads", async (HttpContext http, CancellationToken cancellationToken) =>
{
    var unlocked = DownloadAuth.TryGetSession(http, codes, out _);
    return Results.Ok(await releases.RefreshCatalogAsync(unlocked, cancellationToken));
});

app.MapPost("/activate", async (HttpContext http, ActivateRequest? request, CancellationToken cancellationToken) =>
{
    if (DownloadAuth.TryGetSession(http, codes, out _))
    {
        return Results.Ok(await releases.RefreshCatalogAsync(true, cancellationToken));
    }

    var redeemed = codes.TryRedeem(request?.Code);
    if (redeemed is null)
    {
        await Task.Delay(300, cancellationToken);
        return TypedResults.Json(
            new DownloadCatalog { Unlocked = false, Error = "That code is spent, revoked, or unknown." },
            NetCodeJsonContext.Default.DownloadCatalog,
            statusCode: StatusCodes.Status403Forbidden);
    }

    DownloadAuth.SignIn(http, codes, redeemed.Id);
    return Results.Ok(await releases.RefreshCatalogAsync(true, cancellationToken));
});

app.MapGet("/download/{platform}", async (HttpContext http, string platform, CancellationToken cancellationToken) =>
{
    if (!ReleaseDownloadService.IsPlatform(platform))
    {
        return Results.NotFound();
    }

    if (!DownloadAuth.TryGetSession(http, codes, out _))
    {
        return Results.Unauthorized();
    }

    var url = await releases.ResolveUrlAsync(platform, cancellationToken);
    return url is null
        ? TypedResults.Json(
            new DownloadCatalog { Unlocked = true, Error = "Release assets are unavailable." },
            NetCodeJsonContext.Default.DownloadCatalog,
            statusCode: StatusCodes.Status503ServiceUnavailable)
        : Results.Redirect(url);
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    zones = zoneCount,
    pawns = pawnCount,
    player = contextA.PlayerPawn.Label,
    pool = pool.Count,
    data = dataDir
}));

app.MapPost("/builds", (BuildSnapshot snapshot) =>
{
    pool.Upsert(snapshot);
    return Results.Accepted($"/builds/{snapshot.PlayerId}", snapshot);
});

app.MapGet("/opponent", (int round = 1) =>
{
    var opponent = pool.PickOpponent(round);
    return opponent is null ? Results.NotFound() : Results.Ok(opponent);
});

app.MapPost("/matches", (MatchRequest request) =>
{
    var round = BuildPool.ResolveRound(request.Attacker);
    var attacker = request.Attacker.Round > 0 ? request.Attacker : request.Attacker with { Round = round };
    var defender = request.Defender
                   ?? pool.PickOpponent(round, attacker.PlayerId, attacker.Rating)
                   ?? BuildPool.MirrorOf(attacker);
    if (string.Equals(defender.PlayerId, attacker.PlayerId, StringComparison.Ordinal)
        || string.Equals(defender.PlayerId, $"mirror:{attacker.PlayerId}", StringComparison.Ordinal))
    {
        defender = BuildPool.MirrorOf(attacker);
    }

    var runSeed = attacker.Seed != 0 ? attacker.Seed : Random.Shared.Next();
    var encounterSeed = ArenaSeeds.Encounter(runSeed, round);
    var simulation = DuelSimulator.Simulate(attacker, defender, encounterSeed);
    var result = simulation.Result;
    players.AppendFight(attacker.PlayerId, new ArenaFightRecord
    {
        MatchId = result.MatchId,
        Round = round,
        Attacker = attacker,
        Defender = result.Defender ?? defender,
        EncounterSeed = result.EncounterSeed,
        WinnerPlayerId = result.WinnerPlayerId,
        Ticks = result.Ticks,
        CauseOfDeath = result.CauseOfDeath,
        FoughtAt = DateTimeOffset.UtcNow,
        Analytics = simulation.Analytics
    }, simulation.Log);
    return Results.Ok(result);
});

app.MapPost("/players", (CreatePlayerRequest? request) =>
{
    var profile = players.GetOrCreateProfile(request?.PlayerId, request?.DisplayName, request?.Username);
    return Results.Ok(profile);
});

app.MapGet("/players/{playerId}", (string playerId) =>
{
    var profile = players.GetProfile(playerId);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

app.MapPut("/players/{playerId}", (string playerId, CreatePlayerRequest request) =>
{
    return Results.Ok(players.UpdateProfile(playerId, request.DisplayName, request.Username));
});

app.MapGet("/players/{playerId}/achievements", (string playerId) =>
{
    return Results.Ok(players.GetAchievements(playerId));
});

app.MapPut("/players/{playerId}/achievements", (string playerId, AchievementState state) =>
{
    players.SaveAchievements(playerId, state);
    return Results.Ok(state);
});

app.MapGet("/players/{playerId}/arena", (string playerId) =>
{
    var current = players.GetCurrentArena(playerId);
    return current is null ? Results.NotFound() : Results.Ok(current);
});

app.MapPut("/players/{playerId}/arena", (string playerId, ArenaProgressRecord progress) =>
{
    var stored = progress.PlayerId == playerId ? progress : progress with { PlayerId = playerId };
    return Results.Ok(players.SaveCurrentArena(stored));
});

app.MapPost("/players/{playerId}/arena/start", (string playerId, StartArenaRequest? request) =>
{
    var seed = request?.RunSeed is > 0 ? request.RunSeed.Value : 0;
    return Results.Ok(players.StartArena(playerId, request?.PlayerName, seed));
});

app.MapDelete("/players/{playerId}/arena", (string playerId, bool? victory) =>
{
    var finished = players.FinishCurrent(playerId, victory);
    return finished is null ? Results.NotFound() : Results.Ok(finished);
});

app.MapGet("/players/{playerId}/arena/runs", (string playerId) =>
{
    return Results.Ok(players.ListRuns(playerId));
});

app.MapGet("/players/{playerId}/arena/runs/{runId}", (string playerId, string runId) =>
{
    var run = players.GetRun(playerId, runId);
    return run is null ? Results.NotFound() : Results.Ok(run);
});

app.MapGet("/analytics/fights", () => Results.Ok(analytics.ListFights()));

app.MapGet("/analytics/fights/summary", () => Results.Ok(analytics.Summarize()));

app.MapGet("/analytics/fights/{matchId}/log", (string matchId) =>
{
    var log = analytics.GetLog(matchId);
    return log is null ? Results.NotFound() : Results.Ok(log);
});

app.MapPost("/analytics/backfill", () => Results.Ok(analytics.Backfill()));

app.Run();
