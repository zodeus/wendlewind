using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Server;

public static class PlayerAuth
{
    public static AuthSession SignIn(HttpContext http, AccountStore accounts, AccountRecord account)
    {
        var token = accounts.IssueSession(account.AccountId);
        http.Response.Cookies.Append(AccountStore.CookieName, token, Cookie(http, AccountStore.SessionLifetime));
        return ToSession(account, token);
    }

    public static void SignOut(HttpContext http)
    {
        http.Response.Cookies.Delete(AccountStore.CookieName, new CookieOptions { Path = "/" });
    }

    public static bool TryGetAccount(HttpContext http, AccountStore accounts, out AccountRecord account)
    {
        return accounts.TryValidateSession(ReadToken(http), out account);
    }

    public static IResult? DenyUnlessSignedIn(HttpContext http, AccountStore accounts, out AccountRecord account)
    {
        if (TryGetAccount(http, accounts, out account))
        {
            return null;
        }

        return Results.Unauthorized();
    }

    public static IResult? DenyUnlessOwner(
        HttpContext http,
        AccountStore accounts,
        string? playerId,
        out AccountRecord account)
    {
        var deny = DenyUnlessSignedIn(http, accounts, out account);
        if (deny != null)
        {
            return deny;
        }

        if (!string.IsNullOrWhiteSpace(playerId)
            && !string.Equals(playerId, account.PlayerId, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return null;
    }

    public static AuthSession ToSession(AccountRecord account, string? token = null)
    {
        return new AuthSession
        {
            Authenticated = true,
            AccountId = account.AccountId,
            PlayerId = account.PlayerId,
            Username = account.Username,
            Email = account.Email,
            Token = token
        };
    }

    public static AuthSession Failed(string error, bool authenticated = false)
    {
        return new AuthSession
        {
            Authenticated = authenticated,
            Error = error
        };
    }

    private static string? ReadToken(HttpContext http)
    {
        var header = http.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var bearer = header[prefix.Length..].Trim();
            if (bearer.Length > 0)
            {
                return bearer;
            }
        }

        return http.Request.Cookies.TryGetValue(AccountStore.CookieName, out var cookie) ? cookie : null;
    }

    private static CookieOptions Cookie(HttpContext http, TimeSpan lifetime)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps,
            Path = "/",
            MaxAge = lifetime
        };
    }
}
