using System.IO;
using System.Text.Json;

namespace Wendlewind.Scenes.ArenaScene;

public sealed class PlayerProfile
{
    public string PlayerId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Username { get; set; } = "";

    public const string FileName = "player.json";
    public const int MinUsernameLength = 2;
    public const int MaxUsernameLength = 20;

    public bool HasUsername => IsValidUsername(Username);

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
