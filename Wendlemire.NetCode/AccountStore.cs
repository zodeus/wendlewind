using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.NetCode;

public sealed class AccountStore
{
    public const string CookieName = "wm_user";
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    public const int MinUsernameLength = 2;
    public const int MaxUsernameLength = 20;
    public const int MinPasswordLength = 6;
    public const int MaxPasswordLength = 128;
    public const int MaxEmailLength = 254;

    private const int HashIterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MaxClaimedPlayerIdLength = 64;

    private readonly string _path;
    private readonly object _gate = new();
    private byte[] _secret = [];
    private List<AccountRecord> _accounts = [];

    public AccountStore(string dataDirectory)
    {
        _path = ServerData.AccountsPath(dataDirectory);
        Directory.CreateDirectory(dataDirectory);
        Load();
    }

    public AccountResult Register(string? username, string? password, string? email, string? playerId)
    {
        if (!TryNormalizeUsername(username, out var name, out var usernameError))
        {
            return AccountResult.Fail(usernameError);
        }

        if (!TryNormalizeEmail(email, out var address, out var emailError))
        {
            return AccountResult.Fail(emailError);
        }

        if (!IsValidPassword(password, out var passwordError))
        {
            return AccountResult.Fail(passwordError);
        }

        lock (_gate)
        {
            if (FindByUsernameUnlocked(name) != null)
            {
                return AccountResult.Fail("That username is taken.");
            }

            if (FindByEmailUnlocked(address) != null)
            {
                return AccountResult.Fail("That email is already in use.");
            }

            var now = DateTimeOffset.UtcNow;
            var account = new AccountRecord
            {
                AccountId = Guid.NewGuid().ToString("N"),
                Username = name,
                Email = address,
                PasswordHash = HashPassword(password!),
                PlayerId = ClaimPlayerIdUnlocked(playerId),
                CreatedAt = now
            };
            _accounts.Add(account);
            PersistUnlocked();
            return AccountResult.Succeed(account);
        }
    }

    public AccountResult Login(string? username, string? password)
    {
        if (!TryNormalizeUsername(username, out var name, out var usernameError))
        {
            return AccountResult.Fail(usernameError);
        }

        if (string.IsNullOrEmpty(password))
        {
            return AccountResult.Fail("Wrong username or password.");
        }

        lock (_gate)
        {
            var account = FindByUsernameUnlocked(name);
            if (account == null || !VerifyPassword(password, account.PasswordHash))
            {
                return AccountResult.Fail("Wrong username or password.");
            }

            return AccountResult.Succeed(account);
        }
    }

    public AccountRecord? GetById(string? accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return null;
        }

        lock (_gate)
        {
            return _accounts.Find(item => item.AccountId == accountId);
        }
    }

    public bool IsPlayerIdClaimed(string? playerId)
    {
        var id = (playerId ?? "").Trim();
        if (id.Length == 0)
        {
            return false;
        }

        lock (_gate)
        {
            return _accounts.Exists(item => item.PlayerId == id);
        }
    }

    public string IssueSession(string accountId)
    {
        var issued = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        return $"{accountId}.{issued}.{Convert.ToHexString(Sign($"{accountId}.{issued}"))}";
    }

    public bool TryValidateSession(string? token, out AccountRecord account)
    {
        account = null!;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.', 3);
        if (parts.Length != 3 || !long.TryParse(parts[1], out var issuedSeconds))
        {
            return false;
        }

        var issued = DateTimeOffset.FromUnixTimeSeconds(issuedSeconds);
        if (issued > DateTimeOffset.UtcNow.AddMinutes(5) || DateTimeOffset.UtcNow - issued > SessionLifetime)
        {
            return false;
        }

        byte[] actual;
        try
        {
            actual = Convert.FromHexString(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = Sign($"{parts[0]}.{parts[1]}");
        if (actual.Length != expected.Length
            || !CryptographicOperations.FixedTimeEquals(actual, expected))
        {
            return false;
        }

        lock (_gate)
        {
            var record = _accounts.Find(item => item.AccountId == parts[0]);
            if (record is null)
            {
                return false;
            }

            account = record;
            return true;
        }
    }

    public static bool TryNormalizeUsername(string? username, out string normalized, out string error)
    {
        normalized = (username ?? "").Trim();
        if (normalized.Length < MinUsernameLength || normalized.Length > MaxUsernameLength)
        {
            error = $"Username must be {MinUsernameLength}–{MaxUsernameLength} characters.";
            normalized = "";
            return false;
        }

        error = "";
        return true;
    }

    public static bool TryNormalizeEmail(string? email, out string normalized, out string error)
    {
        var trimmed = (email ?? "").Trim();
        if (trimmed.Length == 0
            || trimmed.Length > MaxEmailLength
            || !MailAddress.TryCreate(trimmed, out var parsed)
            || parsed.Address.IndexOf('@') <= 0
            || parsed.Host.IndexOf('.') < 0)
        {
            error = "Enter a valid email address.";
            normalized = "";
            return false;
        }

        normalized = parsed.Address;
        error = "";
        return true;
    }

    public static bool IsValidPassword(string? password, out string error)
    {
        if (string.IsNullOrEmpty(password)
            || password.Length < MinPasswordLength
            || password.Length > MaxPasswordLength)
        {
            error = $"Password must be {MinPasswordLength}–{MaxPasswordLength} characters.";
            return false;
        }

        error = "";
        return true;
    }

    private string ClaimPlayerIdUnlocked(string? playerId)
    {
        var candidate = (playerId ?? "").Trim();
        if (candidate.Length == 0
            || candidate.Length > MaxClaimedPlayerIdLength
            || _accounts.Exists(item => item.PlayerId == candidate))
        {
            return Guid.NewGuid().ToString("N");
        }

        return candidate;
    }

    private AccountRecord? FindByUsernameUnlocked(string username)
    {
        return _accounts.Find(item =>
            string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    private AccountRecord? FindByEmailUnlocked(string email)
    {
        return _accounts.Find(item =>
            !string.IsNullOrWhiteSpace(item.Email)
            && string.Equals(item.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            HashIterations,
            HashAlgorithmName.SHA256,
            HashSize);
        return $"v1.{HashIterations}.{Convert.ToHexString(salt)}.{Convert.ToHexString(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromHexString(parts[2]);
            expected = Convert.FromHexString(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length == 0 || expected.Length == 0)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private byte[] Sign(string payload)
    {
        lock (_gate)
        {
            return HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(payload));
        }
    }

    private void Load()
    {
        lock (_gate)
        {
            if (File.Exists(_path))
            {
                try
                {
                    var json = File.ReadAllText(_path);
                    var file = JsonSerializer.Deserialize(json, NetCodeJsonContext.Default.AccountFile);
                    if (file != null)
                    {
                        _accounts = file.Accounts ?? [];
                        if (TryParseSecret(file.Secret, out var secret))
                        {
                            _secret = secret;
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    _accounts = [];
                }
            }

            _secret = RandomNumberGenerator.GetBytes(32);
            PersistUnlocked();
        }
    }

    private void PersistUnlocked()
    {
        var file = new AccountFile
        {
            Secret = Convert.ToHexString(_secret),
            Accounts = _accounts.ToList()
        };
        var json = JsonSerializer.Serialize(file, NetCodeJsonContext.Default.AccountFile);
        File.WriteAllText(_path, json);
    }

    private static bool TryParseSecret(string? secret, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        try
        {
            bytes = Convert.FromHexString(secret);
            return bytes.Length >= 16;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed record AccountResult
{
    public AccountRecord? Account { get; init; }
    public string? Error { get; init; }

    public bool Succeeded => Account != null;

    public static AccountResult Succeed(AccountRecord account) => new() { Account = account };

    public static AccountResult Fail(string error) => new() { Error = error };
}
