using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Server;

public static class AuthEndpoints
{
    public static void MapAuth(this WebApplication app, AccountStore accounts, PlayerStore players)
    {
        app.MapPost("/auth/register", async (HttpContext http, AuthRequest? request) =>
        {
            var result = accounts.Register(request?.Username, request?.Password, request?.Email, request?.PlayerId);
            if (!result.Succeeded || result.Account == null)
            {
                var taken = result.Error is "That username is taken." or "That email is already in use.";
                if (taken)
                {
                    await Task.Delay(300);
                }

                return TypedResults.Json(
                    PlayerAuth.Failed(result.Error ?? "Could not register."),
                    NetCodeJsonContext.Default.AuthSession,
                    statusCode: taken ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest);
            }

            players.GetOrCreateProfile(result.Account.PlayerId, result.Account.Username, result.Account.Username);
            return Results.Ok(PlayerAuth.SignIn(http, accounts, result.Account));
        });

        app.MapPost("/auth/login", async (HttpContext http, AuthRequest? request) =>
        {
            var result = accounts.Login(request?.Username, request?.Password);
            if (!result.Succeeded || result.Account == null)
            {
                await Task.Delay(300);
                return TypedResults.Json(
                    PlayerAuth.Failed(result.Error ?? "Wrong username or password."),
                    NetCodeJsonContext.Default.AuthSession,
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(PlayerAuth.SignIn(http, accounts, result.Account));
        });

        app.MapPost("/auth/logout", (HttpContext http) =>
        {
            PlayerAuth.SignOut(http);
            return Results.Ok(new AuthSession { Authenticated = false });
        });

        app.MapGet("/auth/me", (HttpContext http) =>
        {
            if (!PlayerAuth.TryGetAccount(http, accounts, out var account))
            {
                return TypedResults.Json(
                    new AuthSession { Authenticated = false },
                    NetCodeJsonContext.Default.AuthSession,
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(PlayerAuth.ToSession(account));
        });
    }
}
