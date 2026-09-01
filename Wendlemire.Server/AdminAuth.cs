using System.Security.Cryptography;
using System.Text;

namespace Wendlemire.Server;

public sealed class AdminAuth
{
    public const string CookieName = "wm_admin";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

    private readonly byte[] _key;
    private readonly byte[] _passwordHash;

    private AdminAuth(string password)
    {
        var material = Encoding.UTF8.GetBytes("wendlemire-admin:" + password);
        _key = SHA256.HashData(material);
        _passwordHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
    }

    public string StatusMessage { get; private init; } = "";

    public static AdminAuth Create(IHostEnvironment environment)
    {
        var configured = Environment.GetEnvironmentVariable("WENDLEMIRE_ADMIN_PASSWORD");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return new AdminAuth(configured.Trim())
            {
                StatusMessage = "password from WENDLEMIRE_ADMIN_PASSWORD"
            };
        }

        if (environment.IsDevelopment())
        {
            return new AdminAuth("dev")
            {
                StatusMessage = "Development default password 'dev' (set WENDLEMIRE_ADMIN_PASSWORD to override)"
            };
        }

        var generated = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        return new AdminAuth(generated)
        {
            StatusMessage = $"generated password {generated} (set WENDLEMIRE_ADMIN_PASSWORD to persist)"
        };
    }

    public bool TryLogin(string? password)
    {
        var candidate = SHA256.HashData(Encoding.UTF8.GetBytes(password ?? ""));
        return CryptographicOperations.FixedTimeEquals(candidate, _passwordHash);
    }

    public bool IsAuthenticated(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(CookieName, out var cookie) || string.IsNullOrWhiteSpace(cookie))
        {
            return false;
        }

        var parts = cookie.Split('.', 2);
        if (parts.Length != 2 || !long.TryParse(parts[0], out var issuedSeconds))
        {
            return false;
        }

        var issued = DateTimeOffset.FromUnixTimeSeconds(issuedSeconds);
        if (issued > DateTimeOffset.UtcNow.AddMinutes(5) || DateTimeOffset.UtcNow - issued > SessionLifetime)
        {
            return false;
        }

        var expected = Sign(parts[0]);
        try
        {
            var actual = Convert.FromHexString(parts[1]);
            return actual.Length == expected.Length
                   && CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public void SignIn(HttpContext http)
    {
        var issued = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        http.Response.Cookies.Append(CookieName, $"{issued}.{Convert.ToHexString(Sign(issued))}", new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps,
            Path = "/admin",
            MaxAge = SessionLifetime
        });
    }

    public void SignOut(HttpContext http)
    {
        http.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            Path = "/admin"
        });
    }

    private byte[] Sign(string payload) =>
        HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload));
}
