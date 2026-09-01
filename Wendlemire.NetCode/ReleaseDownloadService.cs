using System.Text.Json;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.NetCode;

public sealed class ReleaseDownloadService
{
    public const string WinX64 = "win-x64";
    public const string OsxArm = "osx-arm64";
    public const string OsxIntel = "osx-x64";

    private static readonly (string Id, string Label, string Detail)[] Platforms =
    [
        (WinX64, "Windows", "x64 zip"),
        (OsxArm, "macOS", "Apple Silicon"),
        (OsxIntel, "macOS", "Intel")
    ];

    private readonly string _directory;

    public ReleaseDownloadService(string downloadsDirectory)
    {
        _directory = downloadsDirectory;
        Directory.CreateDirectory(_directory);
    }

    public string DirectoryPath => _directory;

    public int AvailableCount => Platforms.Count(platform => Find(platform.Id) != null);

    public DownloadCatalog Catalog(bool unlocked)
    {
        if (!unlocked)
        {
            return new DownloadCatalog();
        }

        var assets = new List<DownloadAsset>();
        foreach (var platform in Platforms)
        {
            if (Find(platform.Id) == null)
            {
                continue;
            }

            assets.Add(new DownloadAsset
            {
                Id = platform.Id,
                Label = platform.Label,
                Detail = platform.Detail
            });
        }

        return new DownloadCatalog
        {
            Unlocked = true,
            Version = ReadVersion(),
            Error = assets.Count == 0 ? "Client builds are not on this server yet." : null,
            Assets = assets
        };
    }

    public Task<DownloadCatalog> RefreshCatalogAsync(bool unlocked, CancellationToken cancellationToken = default) =>
        Task.FromResult(Catalog(unlocked));

    public ClientDownloadFile? Find(string platformId)
    {
        if (!IsPlatform(platformId) || !Directory.Exists(_directory))
        {
            return null;
        }

        var named = ReadManifestFile(platformId);
        if (named != null)
        {
            return named;
        }

        var matches = Directory.GetFiles(_directory, "*.zip")
            .Where(path => Path.GetFileName(path).Contains(platformId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();
        return matches.Count == 0 ? null : ToDownload(matches[0]);
    }

    public static bool IsPlatform(string platformId) =>
        Platforms.Any(platform => platform.Id.Equals(platformId, StringComparison.OrdinalIgnoreCase));

    private ClientDownloadFile? ReadManifestFile(string platformId)
    {
        var manifestPath = Path.Combine(_directory, "latest.json");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!doc.RootElement.TryGetProperty("files", out var files)
                || files.ValueKind != JsonValueKind.Object
                || !files.TryGetProperty(platformId, out var name)
                || name.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var fileName = name.GetString();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var path = Path.Combine(_directory, Path.GetFileName(fileName));
            return File.Exists(path) ? ToDownload(path) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string? ReadVersion()
    {
        var manifestPath = Path.Combine(_directory, "latest.json");
        if (File.Exists(manifestPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
                if (doc.RootElement.TryGetProperty("version", out var version)
                    && version.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(version.GetString()))
                {
                    return version.GetString();
                }
            }
            catch (JsonException)
            {
            }
        }

        foreach (var platform in Platforms)
        {
            var file = Find(platform.Id);
            if (file == null)
            {
                continue;
            }

            var name = file.FileName;
            var rid = "-" + platform.Id + ".zip";
            if (name.EndsWith(rid, StringComparison.OrdinalIgnoreCase))
            {
                var stem = name[..^rid.Length];
                var dash = stem.LastIndexOf('-');
                if (dash >= 0 && dash < stem.Length - 1)
                {
                    return stem[(dash + 1)..];
                }
            }
        }

        return null;
    }

    private static ClientDownloadFile ToDownload(string path) =>
        new(path, Path.GetFileName(path));
}

public sealed record ClientDownloadFile(string Path, string FileName);
