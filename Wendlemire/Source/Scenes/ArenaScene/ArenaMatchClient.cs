using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Scenes.ArenaScene;

public sealed class ArenaMatchClient : IDisposable
{
    public const string DefaultBaseUrl = "http://localhost:5080";

    private readonly HttpClient _http;

    public ArenaMatchClient(string? baseUrl = null, TimeSpan? timeout = null)
    {
        var url = baseUrl ?? ClientSettings.LoadOrCreate().ResolveBaseUrl();
        _http = new HttpClient
        {
            BaseAddress = new Uri(url.TrimEnd('/') + "/"),
            Timeout = timeout ?? TimeSpan.FromMinutes(2)
        };
        _http.DefaultRequestHeaders.TryAddWithoutValidation(GameVersion.HeaderName, GameVersion.Current);
    }

    public async Task EnsureCompatible()
    {
        HealthStatus health;
        try
        {
            var response = await _http.GetAsync("health");
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("Could not reach the server.");
            }

            health = await Read(response, NetCodeJsonContext.Default.HealthStatus);
        }
        catch (VersionMismatchException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            throw new HttpRequestException("Could not reach the server.");
        }
        catch (HttpRequestException)
        {
            throw new HttpRequestException("Could not reach the server.");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            throw new VersionMismatchException(null);
        }

        if (!GameVersion.Matches(health.Version))
        {
            throw new VersionMismatchException(health.Version);
        }
    }

    public async Task SubmitBuild(BuildSnapshot snapshot)
    {
        var response = await Post("/builds", snapshot, NetCodeJsonContext.Default.BuildSnapshot);
        await EnsureSuccess(response, "Submit build");
    }

    public async Task<CombatResult> RequestMatch(BuildSnapshot attacker)
    {
        var request = new MatchRequest { Attacker = attacker };
        var response = await Post("/matches", request, NetCodeJsonContext.Default.MatchRequest);
        await EnsureSuccess(response, "Match request");
        return await Read(response, NetCodeJsonContext.Default.CombatResult);
    }

    public async Task<PlayerProfileRecord> EnsureProfile(string playerId, string displayName, string? username = null)
    {
        var request = new CreatePlayerRequest
        {
            PlayerId = playerId,
            DisplayName = displayName,
            Username = username
        };
        var response = await Post("/players", request, NetCodeJsonContext.Default.CreatePlayerRequest);
        await EnsureSuccess(response, "Ensure profile");
        return await Read(response, NetCodeJsonContext.Default.PlayerProfileRecord);
    }

    public async Task<bool> HasCurrentArena(string playerId)
    {
        try
        {
            return await GetCurrentArena(playerId) != null;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (VersionMismatchException)
        {
            return false;
        }
    }

    public async Task<ArenaProgressRecord?> GetCurrentArena(string playerId)
    {
        var response = await _http.GetAsync($"players/{playerId}/arena");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccess(response, "Get current arena");
        return await Read(response, NetCodeJsonContext.Default.ArenaProgressRecord);
    }

    public async Task<ArenaProgressRecord> StartArena(string playerId, string playerName, int runSeed = 0)
    {
        var request = new StartArenaRequest
        {
            PlayerName = playerName,
            RunSeed = runSeed > 0 ? runSeed : null
        };
        var response = await Post($"/players/{playerId}/arena/start", request, NetCodeJsonContext.Default.StartArenaRequest);
        await EnsureSuccess(response, "Start arena");
        return await Read(response, NetCodeJsonContext.Default.ArenaProgressRecord);
    }

    public async Task SaveCurrentArena(ArenaProgressRecord progress)
    {
        var response = await Put($"/players/{progress.PlayerId}/arena", progress, NetCodeJsonContext.Default.ArenaProgressRecord);
        await EnsureSuccess(response, "Save arena");
    }

    public async Task<PlayerProfileRecord?> GetProfile(string playerId)
    {
        var response = await _http.GetAsync($"players/{playerId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccess(response, "Get profile");
        return await Read(response, NetCodeJsonContext.Default.PlayerProfileRecord);
    }

    public async Task<CosmeticActionResult> BuyCosmetic(string playerId, string moniker)
    {
        var response = await Post(
            $"/players/{playerId}/cosmetics/buy",
            new CosmeticRequest { Moniker = moniker },
            NetCodeJsonContext.Default.CosmeticRequest);
        await EnsureSuccess(response, "Buy cosmetic");
        return await Read(response, NetCodeJsonContext.Default.CosmeticActionResult);
    }

    public async Task<CosmeticActionResult> EquipCosmetic(string playerId, string moniker)
    {
        var response = await Put(
            $"/players/{playerId}/cosmetics/equip",
            new CosmeticRequest { Moniker = moniker },
            NetCodeJsonContext.Default.CosmeticRequest);
        await EnsureSuccess(response, "Equip cosmetic");
        return await Read(response, NetCodeJsonContext.Default.CosmeticActionResult);
    }

    public async Task<ArenaRunRecord?> FinishArena(string playerId, bool? victory = null)
    {
        var path = victory == null
            ? $"players/{playerId}/arena"
            : $"players/{playerId}/arena?victory={victory.Value}";
        var response = await _http.DeleteAsync(path);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccess(response, "Finish arena");
        return await Read(response, NetCodeJsonContext.Default.ArenaRunRecord);
    }

    public async Task<AchievementState> GetAchievements(string playerId)
    {
        var response = await _http.GetAsync($"players/{playerId}/achievements");
        await EnsureSuccess(response, "Get achievements");
        return await Read(response, NetCodeJsonContext.Default.AchievementState);
    }

    public async Task SaveAchievements(string playerId, AchievementState state)
    {
        var response = await Put($"/players/{playerId}/achievements", state, NetCodeJsonContext.Default.AchievementState);
        await EnsureSuccess(response, "Save achievements");
    }

    private Task<HttpResponseMessage> Post<T>(string path, T value, JsonTypeInfo<T> typeInfo)
    {
        return Send(HttpMethod.Post, path, value, typeInfo);
    }

    private Task<HttpResponseMessage> Put<T>(string path, T value, JsonTypeInfo<T> typeInfo)
    {
        return Send(HttpMethod.Put, path, value, typeInfo);
    }

    private Task<HttpResponseMessage> Send<T>(HttpMethod method, string path, T value, JsonTypeInfo<T> typeInfo)
    {
        var json = JsonSerializer.Serialize(value, typeInfo);
        var request = new HttpRequestMessage(method, path.TrimStart('/'))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return _http.SendAsync(request);
    }

    private static async Task<T> Read<T>(HttpResponseMessage response, JsonTypeInfo<T> typeInfo)
        where T : class
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, typeInfo)
               ?? throw new InvalidOperationException("Server returned an empty response.");
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, string action)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        var mismatch = TryReadVersionMismatch(body);
        if (mismatch != null || (int)response.StatusCode == 426)
        {
            throw new VersionMismatchException(mismatch?.ServerVersion);
        }

        throw new HttpRequestException($"{action} failed: {(int)response.StatusCode} {response.ReasonPhrase}");
    }

    private static VersionMismatchError? TryReadVersionMismatch(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            var error = JsonSerializer.Deserialize(body, NetCodeJsonContext.Default.VersionMismatchError);
            if (error?.Code == "version_mismatch" || !string.IsNullOrEmpty(error?.ServerVersion))
            {
                return error;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
