using Wendlemire.NetCode;
using Xunit;

namespace Wendlemire.Tests;

public class AccountStoreTests
{
    [Fact]
    public void RegistersLogsInAndRejectsBadPasswords()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-accounts-{Guid.NewGuid():N}");
        try
        {
            var store = new AccountStore(dir);
            var created = store.Register("Hawk", "secret1", "hawk@example.com", "player-one");
            Assert.True(created.Succeeded);
            Assert.Equal("Hawk", created.Account!.Username);
            Assert.Equal("hawk@example.com", created.Account.Email);
            Assert.Equal("player-one", created.Account.PlayerId);
            Assert.False(string.IsNullOrWhiteSpace(created.Account.PasswordHash));
            Assert.DoesNotContain("secret1", created.Account.PasswordHash, StringComparison.Ordinal);

            Assert.Equal("That username is taken.", store.Register("hawk", "secret2", "other@example.com", null).Error);
            Assert.Equal("That email is already in use.", store.Register("Other", "secret2", "HAWK@example.com", null).Error);
            Assert.Equal("Enter a valid email address.", store.Register("Other", "secret2", "not-an-email", null).Error);
            Assert.Equal("Wrong username or password.", store.Login("Hawk", "nope").Error);
            Assert.Null(store.Register("x", "secret1", "ok@example.com", null).Account);
            Assert.Null(store.Register("ValidName", "ab", "ok@example.com", null).Account);

            var login = store.Login("hawk", "secret1");
            Assert.True(login.Succeeded);
            Assert.Equal(created.Account.AccountId, login.Account!.AccountId);

            var session = store.IssueSession(login.Account.AccountId);
            Assert.True(store.TryValidateSession(session, out var fromSession));
            Assert.Equal(login.Account.AccountId, fromSession.AccountId);

            var reloaded = new AccountStore(dir);
            Assert.True(reloaded.Login("Hawk", "secret1").Succeeded);
            Assert.True(reloaded.TryValidateSession(session, out var restored));
            Assert.Equal("player-one", restored.PlayerId);
            Assert.True(reloaded.IsPlayerIdClaimed("player-one"));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void ClaimsUnusedPlayerIdAndMintsWhenTaken()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-accounts-{Guid.NewGuid():N}");
        try
        {
            var store = new AccountStore(dir);
            var first = store.Register("alice", "secret1", "alice@example.com", "shared-id");
            var second = store.Register("bob", "secret1", "bob@example.com", "shared-id");
            Assert.True(first.Succeeded);
            Assert.True(second.Succeeded);
            Assert.Equal("shared-id", first.Account!.PlayerId);
            Assert.NotEqual("shared-id", second.Account!.PlayerId);
            Assert.False(string.IsNullOrWhiteSpace(second.Account.PlayerId));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void RejectsUnknownSessions()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-accounts-{Guid.NewGuid():N}");
        try
        {
            var store = new AccountStore(dir);
            Assert.False(store.TryValidateSession(null, out _));
            Assert.False(store.TryValidateSession("", out _));
            Assert.False(store.TryValidateSession("bogus", out _));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
