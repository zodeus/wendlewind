using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wendlewind.NetCode.Contracts;

namespace Wendlewind.NetCode;

public sealed class ActivationCodeStore
{
    public const string CookieName = "ww_dl";
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    public const int MaxGenerate = 25;

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly string _path;
    private readonly object _gate = new();
    private byte[] _secret = [];
    private List<ActivationCodeRecord> _codes = [];

    public ActivationCodeStore(string dataDirectory)
    {
        _path = ServerData.CodesPath(dataDirectory);
        Directory.CreateDirectory(dataDirectory);
        Load();
    }

    public IReadOnlyList<ActivationCodeRecord> List()
    {
        lock (_gate)
        {
            return _codes
                .OrderByDescending(code => code.CreatedAt)
                .ToList();
        }
    }

    public ActivationCodeSummary Summarize()
    {
        lock (_gate)
        {
            return new ActivationCodeSummary
            {
                Total = _codes.Count,
                Unused = _codes.Count(IsUnused),
                Redeemed = _codes.Count(code => code.RedeemedAt != null),
                Revoked = _codes.Count(code => code.RevokedAt != null)
            };
        }
    }

    public List<ActivationCodeRecord> Generate(int count, string? note = null)
    {
        var n = Math.Clamp(count, 1, MaxGenerate);
        var label = string.IsNullOrWhiteSpace(note) ? "" : note.Trim();
        var created = new List<ActivationCodeRecord>(n);
        var now = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            var existing = new HashSet<string>(_codes.Select(code => Normalize(code.Code)), StringComparer.Ordinal);
            for (var i = 0; i < n; i++)
            {
                string value;
                do
                {
                    value = NewCode();
                }
                while (!existing.Add(Normalize(value)));

                var record = new ActivationCodeRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Code = value,
                    Note = label,
                    CreatedAt = now
                };
                _codes.Add(record);
                created.Add(record);
            }

            PersistUnlocked();
        }

        return created;
    }

    public ActivationCodeRecord? TryRedeem(string? code)
    {
        var normalized = Normalize(code);
        if (normalized.Length == 0)
        {
            return null;
        }

        lock (_gate)
        {
            var index = _codes.FindIndex(item => Normalize(item.Code) == normalized);
            if (index < 0)
            {
                return null;
            }

            var existing = _codes[index];
            if (!IsUnused(existing))
            {
                return null;
            }

            var redeemed = existing with { RedeemedAt = DateTimeOffset.UtcNow };
            _codes[index] = redeemed;
            PersistUnlocked();
            return redeemed;
        }
    }

    public ActivationCodeRecord? Revoke(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        lock (_gate)
        {
            var index = _codes.FindIndex(item => item.Id == id);
            if (index < 0)
            {
                return null;
            }

            var existing = _codes[index];
            if (existing.RevokedAt != null)
            {
                return existing;
            }

            var revoked = existing with { RevokedAt = DateTimeOffset.UtcNow };
            _codes[index] = revoked;
            PersistUnlocked();
            return revoked;
        }
    }

    public string IssueSession(string codeId)
    {
        var issued = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        return $"{codeId}.{issued}.{Convert.ToHexString(Sign($"{codeId}.{issued}"))}";
    }

    public bool TryValidateSession(string? cookie, out string codeId)
    {
        codeId = "";
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return false;
        }

        var parts = cookie.Split('.', 3);
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
            var record = _codes.Find(item => item.Id == parts[0]);
            if (record is null || record.RedeemedAt == null || record.RevokedAt != null)
            {
                return false;
            }

            codeId = record.Id;
            return true;
        }
    }

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "";
        }

        var chars = code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray();
        return new string(chars);
    }

    public static string Format(string code)
    {
        var normalized = Normalize(code);
        if (normalized.Length == 0)
        {
            return "";
        }

        var parts = new List<string>();
        for (var i = 0; i < normalized.Length; i += 4)
        {
            parts.Add(normalized.Substring(i, Math.Min(4, normalized.Length - i)));
        }

        return string.Join('-', parts);
    }

    private static bool IsUnused(ActivationCodeRecord code) =>
        code.RedeemedAt == null && code.RevokedAt == null;

    private static string NewCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        var chars = new char[12];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return Format(new string(chars));
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
                    var file = JsonSerializer.Deserialize(json, NetCodeJsonContext.Default.ActivationCodeFile);
                    if (file != null)
                    {
                        _codes = file.Codes ?? [];
                        if (TryParseSecret(file.Secret, out var secret))
                        {
                            _secret = secret;
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    _codes = [];
                }
            }

            _secret = RandomNumberGenerator.GetBytes(32);
            PersistUnlocked();
        }
    }

    private void PersistUnlocked()
    {
        var file = new ActivationCodeFile
        {
            Secret = Convert.ToHexString(_secret),
            Codes = _codes.ToList()
        };
        var json = JsonSerializer.Serialize(file, NetCodeJsonContext.Default.ActivationCodeFile);
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
