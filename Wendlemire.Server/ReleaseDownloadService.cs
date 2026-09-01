using System.Text.Json;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Server;

public sealed class ReleaseDownloadService
{
    public const string WinX64 = "win-x64";
    public const string OsxArm = "osx-arm64";
    public const string OsxIntel = "osx-x64";

    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(2);
    private static readonly (string Id, string Label, string Detail, string Needle)[] Platforms =
    [
        (WinX64, "Windows", "x64 zip", WinX64),
        (OsxArm, "macOS", "Apple Silicon", OsxArm),
        (OsxIntel, "macOS", "Intel", OsxIntel)
    ];

    private readonly HttpClient _http;
    private readonly object _gate = new();
    private DateTimeOffset _fetchedAt;
    private string? _version;
    private Dictionary<string, string> _urls = new(StringComparer.OrdinalIgnoreCase);

    public ReleaseDownloadService(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "WendlemireServer");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
    }

    public DownloadCatalog Catalog(bool unlocked)
    {
        return new DownloadCatalog
        {
            Unlocked = unlocked,
            Version = unlocked ? _version : null,
            Assets = unlocked
                ? Platforms.Select(platform => new DownloadAsset
                {
                    Id = platform.Id,
                    Label = platform.Label,
                    Detail = platform.Detail
                }).ToList()
                : []
        };
    }

    public async Task<DownloadCatalog> RefreshCatalogAsync(bool unlocked, CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
        return Catalog(unlocked);
    }

    public async Task<string?> ResolveUrlAsync(string platformId, CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
        lock (_gate)
        {
            return _urls.TryGetValue(platformId, out var url) ? url : null;
        }
    }

    public static bool IsPlatform(string platformId) =>
        Platforms.Any(platform => platform.Id.Equals(platformId, StringComparison.OrdinalIgnoreCase));

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_urls.Count > 0 && DateTimeOffset.UtcNow - _fetchedAt < CacheFor)
            {
                return;
            }
        }

        using var response = await _http.GetAsync(
            "https://api.github.com/repos/zodeus/wendlemire/releases/latest",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var version = root.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(version)
            && root.TryGetProperty("tag_name", out var tag)
            && tag.ValueKind == JsonValueKind.String)
        {
            version = tag.GetString();
        }

        var urls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var file = asset.TryGetProperty("name", out var fileName) ? fileName.GetString() ?? "" : "";
                var url = asset.TryGetProperty("browser_download_url", out var download)
                    ? download.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                foreach (var platform in Platforms)
                {
                    if (file.Contains(platform.Needle, StringComparison.OrdinalIgnoreCase))
                    {
                        urls[platform.Id] = url;
                    }
                }
            }
        }

        lock (_gate)
        {
            _fetchedAt = DateTimeOffset.UtcNow;
            _version = version;
            if (urls.Count > 0)
            {
                _urls = urls;
            }
        }
    }
}
