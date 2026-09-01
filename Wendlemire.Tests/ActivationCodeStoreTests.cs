using Wendlemire.NetCode;
using Xunit;

namespace Wendlemire.Tests;

public class ActivationCodeStoreTests
{
    [Fact]
    public void GeneratesRedeemsAndRejectsSpentCodes()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-codes-{Guid.NewGuid():N}");
        try
        {
            var store = new ActivationCodeStore(dir);
            var created = store.Generate(2, "playtesters");
            Assert.Equal(2, created.Count);
            Assert.All(created, code =>
            {
                Assert.Matches(@"^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$", code.Code);
                Assert.Equal("playtesters", code.Note);
                Assert.Null(code.RedeemedAt);
            });
            Assert.Equal(2, store.Summarize().Unused);

            var first = created[0];
            var redeemed = store.TryRedeem(first.Code.ToLowerInvariant().Replace("-", " "));
            Assert.NotNull(redeemed);
            Assert.Equal(first.Id, redeemed.Id);
            Assert.NotNull(redeemed.RedeemedAt);
            Assert.Null(store.TryRedeem(first.Code));
            Assert.Equal(1, store.Summarize().Unused);

            var session = store.IssueSession(redeemed.Id);
            Assert.True(store.TryValidateSession(session, out var sessionId));
            Assert.Equal(redeemed.Id, sessionId);

            store.Revoke(redeemed.Id);
            Assert.False(store.TryValidateSession(session, out _));

            store.Revoke(created[1].Id);
            Assert.Null(store.TryRedeem(created[1].Code));

            var reloaded = new ActivationCodeStore(dir);
            Assert.Equal(2, reloaded.List().Count);
            Assert.Equal(0, reloaded.Summarize().Unused);
            Assert.False(reloaded.TryValidateSession(session, out _));
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
    public void RejectsUnknownAndEmptyCodes()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ww-codes-{Guid.NewGuid():N}");
        try
        {
            var store = new ActivationCodeStore(dir);
            Assert.Null(store.TryRedeem(null));
            Assert.Null(store.TryRedeem(""));
            Assert.Null(store.TryRedeem("not-a-real-code"));
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
