namespace Wendlemire.NetCode;

/// <summary>
/// Shared client/server version. Bump this when shipping a new client so older builds cannot connect.
/// </summary>
public static class GameVersion
{
    public const string Current = "0.1a";
    public const string HeaderName = "X-Wendlemire-Version";
    public const string DownloadUrl = "https://wendlemire.com";

    public static bool Matches(string? version)
    {
        return string.Equals(Current, version?.Trim(), StringComparison.Ordinal);
    }

    public static bool RequiresClientVersion(string? path)
    {
        var value = (path ?? "").Trim();
        if (value.Length == 0 || value == "/")
        {
            return false;
        }

        var pathOnly = value.Split('?', 2)[0];
        if (IsPublicPath(pathOnly))
        {
            return false;
        }

        var slash = pathOnly.LastIndexOf('/');
        var name = slash >= 0 ? pathOnly[(slash + 1)..] : pathOnly;
        return !name.Contains('.');
    }

    public static string Coalesce(params string?[] versions)
    {
        foreach (var version in versions)
        {
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version.Trim();
            }
        }

        return Current;
    }

    public static string MismatchMessage(string? clientVersion, string? serverVersion = null)
    {
        var client = string.IsNullOrWhiteSpace(clientVersion) ? "unknown" : clientVersion.Trim();
        var server = string.IsNullOrWhiteSpace(serverVersion) ? Current : serverVersion.Trim();
        return $"This client (v{client}) does not match the server (v{server}). Download the latest client from {DownloadUrl}";
    }

    private static bool IsPublicPath(string path)
    {
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/activate", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/download", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class VersionMismatchException : Exception
{
    public VersionMismatchException(string? serverVersion, string? clientVersion = null)
        : base(GameVersion.MismatchMessage(clientVersion ?? GameVersion.Current, serverVersion))
    {
        ServerVersion = serverVersion;
        ClientVersion = clientVersion ?? GameVersion.Current;
    }

    public string? ServerVersion { get; }
    public string ClientVersion { get; }
}
