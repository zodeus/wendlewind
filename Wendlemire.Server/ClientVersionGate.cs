using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Server;

public static class ClientVersionGate
{
    public static void UseClientVersionGate(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (!GameVersion.RequiresClientVersion(context.Request.Path.Value))
            {
                await next();
                return;
            }

            var clientVersion = context.Request.Headers[GameVersion.HeaderName].ToString();
            if (GameVersion.Matches(clientVersion))
            {
                await next();
                return;
            }

            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await context.Response.WriteAsJsonAsync(
                new VersionMismatchError
                {
                    Error = GameVersion.MismatchMessage(clientVersion, GameVersion.Current),
                    Code = "version_mismatch",
                    ServerVersion = GameVersion.Current,
                    ClientVersion = string.IsNullOrWhiteSpace(clientVersion) ? null : clientVersion
                },
                NetCodeJsonContext.Default.VersionMismatchError);
        });
    }
}
