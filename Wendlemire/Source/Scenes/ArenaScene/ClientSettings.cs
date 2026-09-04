using System.IO;
using System.Text.Json;

namespace Wendlemire.Scenes.ArenaScene;

public sealed class ClientSettings
{
    public const string FileName = "client.json";
    public const string DefaultHost = "localhost";
    public const int DefaultPort = 5080;

    public string ServerHost { get; set; } = DefaultHost;
    public bool FullScreen { get; set; } = false;
    public float MasterVolume { get; set; } = 1f;
    public float SfxVolume { get; set; } = 1f;
    public bool AudioMuted { get; set; } = false;

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, FileName);

    public static ClientSettings LoadOrCreate(string? path = null)
    {
        path ??= DefaultPath;
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<ClientSettings>(File.ReadAllText(path));
            if (loaded != null)
            {
                if (string.IsNullOrWhiteSpace(loaded.ServerHost))
                {
                    loaded.ServerHost = DefaultHost;
                }

                loaded.MasterVolume = Math.Clamp(loaded.MasterVolume, 0f, 1f);
                loaded.SfxVolume = Math.Clamp(loaded.SfxVolume, 0f, 1f);
                return loaded;
            }
        }

        return new ClientSettings();
    }

    public void SetServerHost(string host, string? path = null)
    {
        var trimmed = host.Trim();
        ServerHost = string.IsNullOrEmpty(trimmed) ? DefaultHost : trimmed;
        Save(path);
    }

    public void SetFullScreen(bool fullScreen, string? path = null)
    {
        FullScreen = fullScreen;
        Save(path);
    }

    public void SetAudioMuted(bool muted, string? path = null)
    {
        AudioMuted = muted;
        Save(path);
    }

    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    public string ResolveBaseUrl()
    {
        var host = ServerHost?.Trim();
        if (string.IsNullOrEmpty(host))
        {
            var env = Environment.GetEnvironmentVariable("WENDLEMIRE_SERVER_URL");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env.Trim().TrimEnd('/');
            }

            host = DefaultHost;
        }

        return ToBaseUrl(host);
    }

    public static string ToBaseUrl(string host)
    {
        var value = host.Trim().TrimEnd('/');
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (value.Contains(':'))
        {
            return $"http://{value}";
        }

        if (IsLocalHost(value))
        {
            return $"http://{value}:{DefaultPort}";
        }

        return $"http://{value}";
    }

    private static bool IsLocalHost(string host)
    {
        return host.Equals(DefaultHost, StringComparison.OrdinalIgnoreCase)
               || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }
}
