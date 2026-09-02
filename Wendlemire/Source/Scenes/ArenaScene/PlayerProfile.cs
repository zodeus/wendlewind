using System.IO;
using System.Text.Json;
using Wendlemire.NetCode;

namespace Wendlemire.Scenes.ArenaScene;

public sealed class PlayerProfile
{
    public string PlayerId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string SessionToken { get; set; } = "";

    public const string FileName = "player.json";
    public const int MinUsernameLength = AccountStore.MinUsernameLength;
    public const int MaxUsernameLength = AccountStore.MaxUsernameLength;
    public const int MinPasswordLength = AccountStore.MinPasswordLength;

    public bool HasUsername => IsValidUsername(Username);
    public bool HasSession => HasUsername && !string.IsNullOrWhiteSpace(SessionToken);

    public static PlayerProfile LoadOrCreate(string path = FileName)
    {
        if (File.Exists(path))
        {
            var loaded = JsonSerializer.Deserialize<PlayerProfile>(File.ReadAllText(path));
            if (loaded != null && !string.IsNullOrWhiteSpace(loaded.PlayerId))
            {
                if (!string.IsNullOrWhiteSpace(loaded.Username) && string.IsNullOrWhiteSpace(loaded.DisplayName))
                {
                    loaded.DisplayName = loaded.Username;
                }

                return loaded;
            }
        }

        var profile = new PlayerProfile
        {
            PlayerId = Guid.NewGuid().ToString("N")
        };
        profile.Save(path);
        return profile;
    }

    public void SetUsername(string username)
    {
        Username = username.Trim();
        DisplayName = Username;
        Save();
    }

    public void ApplyAccount(string playerId, string username, string? email, string? sessionToken)
    {
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            PlayerId = playerId.Trim();
        }

        Username = username.Trim();
        DisplayName = Username;
        if (!string.IsNullOrWhiteSpace(email))
        {
            Email = email.Trim();
        }

        SessionToken = sessionToken?.Trim() ?? "";
        Save();
    }

    public void ClearSession()
    {
        SessionToken = "";
        Save();
    }

    public void Save(string path = FileName)
    {
        if (string.IsNullOrWhiteSpace(PlayerId))
        {
            PlayerId = Guid.NewGuid().ToString("N");
        }

        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    public static bool IsValidUsername(string? username)
    {
        var trimmed = username?.Trim() ?? "";
        return trimmed.Length >= MinUsernameLength && trimmed.Length <= MaxUsernameLength;
    }
}
