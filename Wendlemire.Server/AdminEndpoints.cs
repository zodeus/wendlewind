using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Server;

public static class AdminEndpoints
{
    public static void MapAdmin(
        this WebApplication app,
        AdminAuth auth,
        PlayerStore players,
        BuildPool pool,
        FightAnalyticsService analytics,
        ActivationCodeStore codes)
    {
        app.MapGet("/admin", () => Results.Redirect("/admin/index.html"));

        var api = app.MapGroup("/admin/api");
        api.AddEndpointFilter(async (context, next) =>
        {
            var path = context.HttpContext.Request.Path;
            if (path.StartsWithSegments("/admin/api/login") || path.StartsWithSegments("/admin/api/session"))
            {
                return await next(context);
            }

            if (!auth.IsAuthenticated(context.HttpContext.Request))
            {
                return Results.Unauthorized();
            }

            return await next(context);
        });

        api.MapGet("/session", (HttpContext http) =>
            Results.Ok(new AdminSession { Authenticated = auth.IsAuthenticated(http.Request) }));

        api.MapPost("/login", async (HttpContext http, AdminLoginRequest? request) =>
        {
            if (!auth.TryLogin(request?.Password))
            {
                await Task.Delay(300);
                return Results.Unauthorized();
            }

            auth.SignIn(http);
            return Results.Ok(new AdminSession { Authenticated = true });
        });

        api.MapPost("/logout", (HttpContext http) =>
        {
            auth.SignOut(http);
            return Results.Ok(new AdminSession { Authenticated = false });
        });

        api.MapGet("/overview", () =>
        {
            var snapshot = pool.Snapshot();
            var summary = codes.Summarize();
            return Results.Ok(players.SummarizeAdmin(snapshot.Count, snapshot.Rounds, analytics.Summarize()) with
            {
                ActivationCodes = summary.Total,
                UnusedCodes = summary.Unused
            });
        });

        api.MapGet("/codes", () => Results.Ok(codes.List()));

        api.MapPost("/codes", (CreateActivationCodesRequest? request) =>
        {
            var created = codes.Generate(request?.Count ?? 1, request?.Note);
            return Results.Ok(created);
        });

        api.MapPost("/codes/{id}/revoke", (string id) =>
        {
            var revoked = codes.Revoke(id);
            return revoked is null ? Results.NotFound() : Results.Ok(revoked);
        });

        api.MapDelete("/codes/{id}", (string id) =>
            codes.Delete(id) ? Results.NoContent() : Results.NotFound());

        api.MapGet("/players", () => Results.Ok(players.ListPlayers()));

        api.MapGet("/players/{playerId}", (string playerId) =>
        {
            var detail = players.GetPlayerDetail(playerId);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        api.MapDelete("/players/{playerId}", (string playerId) =>
        {
            if (!players.DeletePlayer(playerId))
            {
                return Results.NotFound();
            }

            pool.RemovePlayer(playerId);
            return Results.NoContent();
        });

        api.MapGet("/runs", () => Results.Ok(players.ListAllRunRows()));

        api.MapGet("/runs/{playerId}/{runId}", (string playerId, string runId) =>
        {
            var run = players.GetRun(playerId, runId);
            return run is null ? Results.NotFound() : Results.Ok(run);
        });

        api.MapGet("/fights", () => Results.Ok(analytics.ListFights()));

        api.MapGet("/fights/summary", () => Results.Ok(analytics.Summarize()));

        api.MapPost("/fights/verify", () => Results.Ok(analytics.VerifyRecorded()));

        api.MapGet("/fights/{matchId}/log", (string matchId) =>
        {
            var log = analytics.GetLog(matchId);
            return log is null ? Results.NotFound() : Results.Ok(log);
        });

        api.MapGet("/pool", () =>
        {
            var snapshot = pool.Snapshot();
            var labels = players.PlayerLabels();
            return Results.Ok(snapshot with
            {
                Builds = snapshot.Builds
                    .Select(build => build with
                    {
                        Username = labels.GetValueOrDefault(build.PlayerId)
                    })
                    .ToList()
            });
        });
    }
}
