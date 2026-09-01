using Wendlewind.NetCode;

namespace Wendlewind.Server;

public static class DownloadAuth
{
    public static void SignIn(HttpContext http, ActivationCodeStore codes, string codeId)
    {
        http.Response.Cookies.Append(ActivationCodeStore.CookieName, codes.IssueSession(codeId), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps,
            Path = "/",
            MaxAge = ActivationCodeStore.SessionLifetime
        });
    }

    public static bool TryGetSession(HttpContext http, ActivationCodeStore codes, out string codeId)
    {
        codeId = "";
        return http.Request.Cookies.TryGetValue(ActivationCodeStore.CookieName, out var cookie)
               && codes.TryValidateSession(cookie, out codeId);
    }
}
