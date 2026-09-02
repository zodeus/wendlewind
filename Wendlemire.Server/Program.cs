using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.Definitions.Loader;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;
using Wendlemire.Server;
using Wendlemire.Sim;
using Wendlemire.Sim.Arena;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Pawns;
using Wendlemire.Sim.Zones;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWendlemireSimulation();
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
var accounts = new AccountStore(dataDir);
var releases = new ReleaseDownloadService(ServerData.DownloadsDir(dataDir));
var adminAuth = AdminAuth.Create(app.Environment);
Console.WriteLine($"Version: {GameVersion.Current}");
Console.WriteLine($"Data directory: {dataDir}");
Console.WriteLine($"Admin: {adminAuth.StatusMessage}");
Console.WriteLine($"Downloads: {releases.AvailableCount} client build(s) in {releases.DirectoryPath}");

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseClientVersionGate();
app.MapAdmin(adminAuth, players, pool, analytics, codes);
app.MapAuth(accounts, players);

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

app.MapGet("/download/{platform}", (HttpContext http, string platform) =>
{
    if (!ReleaseDownloadService.IsPlatform(platform))
    {
        return Results.NotFound();
    }

    if (!DownloadAuth.TryGetSession(http, codes, out _))
    {
        return Results.Unauthorized();
    }

    var file = releases.Find(platform);
    return file is null
        ? TypedResults.Json(
            new DownloadCatalog { Unlocked = true, Error = "Client builds are not on this server yet." },
            NetCodeJsonContext.Default.DownloadCatalog,
            statusCode: StatusCodes.Status503ServiceUnavailable)
        : Results.File(file.Path, "application/zip", file.FileName, enableRangeProcessing: true);
});

app.MapGet("/health", () => Results.Ok(new HealthStatus
{
    Status = "ok",
    Version = GameVersion.Current,
    Zones = zoneCount,
    Pawns = pawnCount,
    Player = contextA.PlayerPawn.Label,
    Pool = pool.Count,
    Data = dataDir
}));

app.MapPost("/builds", (HttpContext http, BuildSnapshot snapshot) =>
{
    if (PlayerAuth.DenyUnlessOwner(http, accounts, snapshot.PlayerId, out var account) is { } deny)
    {
        return deny;
    }

    var stamped = players.StampCosmetics(snapshot with { PlayerId = account.PlayerId });
    pool.Upsert(stamped);
    return Results.Accepted($"/builds/{stamped.PlayerId}", stamped);
});

app.MapGet("/opponent", (int round = 1) =>
{
    var opponent = pool.PickOpponent(round);
    return opponent is null ? Results.NotFound() : Results.Ok(opponent);
});

app.MapPost("/matches", (HttpContext http, MatchRequest request) =>
{
    if (PlayerAuth.DenyUnlessOwner(http, accounts, request.Attacker.PlayerId, out var account) is { } deny)
    {
        return deny;
    }

    var attackerSnapshot = request.Attacker with { PlayerId = account.PlayerId };
    var round = BuildPool.ResolveRound(attackerSnapshot);
    var attacker = players.StampCosmetics(
        attackerSnapshot.Round > 0 ? attackerSnapshot : attackerSnapshot with { Round = round });
    var defender = request.Defender
                   ?? pool.PickOpponent(round, attacker.PlayerId, attacker.Rating)
                   ?? BuildPool.MirrorOf(attacker);
    defender = players.StampCosmetics(defender);
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
        Analytics = simulation.Analytics,
        Version = GameVersion.Current
    }, simulation.Log);
    return Results.Ok(result);
});

app.MapPost("/players", (HttpContext http, CreatePlayerRequest? request) =>
{
    if (PlayerAuth.DenyUnlessSignedIn(http, accounts, out var account) is { } deny)
    {
        return deny;
    }

    var profile = players.GetOrCreateProfile(account.PlayerId, request?.DisplayName, account.Username);
    return Results.Ok(profile);
});

app.MapGet("/players/{playerId}", (string playerId) =>
{
    var profile = players.GetProfile(playerId);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

app.MapPut("/players/{playerId}", (HttpContext http, string playerId, CreatePlayerRequest request) =>
{
    if (PlayerAuth.DenyUnlessOwner(http, accounts, playerId, out var account) is { } deny)
    {
        return deny;
    }

    return Results.Ok(players.UpdateProfile(account.PlayerId, request.DisplayName, account.Username));
});

app.MapPost("/players/{playerId}/cosmetics/buy", (HttpContext http, string playerId, CosmeticRequest? request) =>
{
    if (PlayerAuth.DenyUnlessOwner(http, accounts, playerId, out var account) is { } deny)
    {
        return deny;
    }

    return Results.Ok(players.BuyCosmetic(account.PlayerId, request?.Moniker));
});

app.MapPut("/players/{playerId}/cosmetics/equip", (HttpContext http, string playerId, CosmeticRequest? request) =>
{
    if (PlayerAuth.DenyUnlessOwner(http, accounts, playerId, out var account) is { } deny)
    {
        return deny;
    }

    return Results.Ok(players.EquipCosmetic(account.PlayerId, request?.Moniker));
});

app.MapGet("/players/{playerId}/achievements", (string playerId) =>
{
    return Results.Ok(players.GetAchievements(playerId));
});

app.MapPut("/players/{playerId}/achievements", (HttpContext http, string playerId, AchievementState state) =>
{
    if (PlayerAuth.DenyUnlessOwner(http, accounts, playerId, out var account) is { } deny)
    {
        return deny;
    }

    players.SaveAchievements(account.PlayerId, state);
    return Results.Ok(state);
});

app.MapGet("/players/{playerId}/arena", (string playerId) =>
{
    var current = players.GetCurrentArena(playerId);
    return current is null ? Results.NotFound() : Results.Ok(current);
});

app.MapPut("/players/{playerId}/arena", (HttpContext http, string playerId, ArenaProgressRecord progress) =>
{
    if (PlayerAuth.DenyUnlessOwner(http, accounts, playerId, out var account) is { } deny)
    {
        return deny;
    }

    var stored = progress with { PlayerId = account.PlayerId };
    return Results.Ok(players.SaveCurrentArena(stored));
});

app.MapPost("/players/{playerId}/arena/start", (HttpContext http, string playerId, StartArenaRequest? request) =>
{
    if (PlayerAuth.DenyUnlessOwner(http, accounts, playerId, out var account) is { } deny)
    {
        return deny;
    }

    var seed = request?.RunSeed is > 0 ? request.RunSeed.Value : 0;
    return Results.Ok(players.StartArena(account.PlayerId, request?.PlayerName, seed));
});

app.MapDelete("/players/{playerId}/arena", (HttpContext http, string playerId, bool? victory) =>
{
    if (PlayerAuth.DenyUnlessOwner(http, accounts, playerId, out var account) is { } deny)
    {
        return deny;
    }

    var finished = players.FinishCurrent(account.PlayerId, victory);
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
