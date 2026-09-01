using Wendlewind.NetCode;
using Xunit;

namespace Wendlewind.Tests;

public class ArenaRankTests
{
    [Theory]
    [InlineData(0, 0, ArenaLeague.Unranked, 800)]
    [InlineData(800, 3, ArenaLeague.Unranked, 800)]
    [InlineData(500, 5, ArenaLeague.Bronze, 500)]
    [InlineData(666, 5, ArenaLeague.Bronze, 666)]
    [InlineData(667, 5, ArenaLeague.Bronze, 667)]
    [InlineData(834, 5, ArenaLeague.Bronze, 834)]
    [InlineData(1000, 5, ArenaLeague.Silver, 1000)]
    [InlineData(1199, 5, ArenaLeague.Silver, 1199)]
    [InlineData(1200, 5, ArenaLeague.Gold, 1200)]
    [InlineData(1400, 5, ArenaLeague.Platinum, 1400)]
    [InlineData(1600, 5, ArenaLeague.Diamond, 1600)]
    [InlineData(1849, 5, ArenaLeague.Diamond, 1849)]
    [InlineData(1850, 5, ArenaLeague.Legend, 1850)]
    public void FromRatingMapsBands(int rating, int ratedRuns, ArenaLeague league, int shown)
    {
        var display = ArenaRank.FromRating(rating, ratedRuns);
        Assert.Equal(league, display.League);
        Assert.Equal(shown, display.Rating);
    }

    [Fact]
    public void BronzeDivisionsFollowPlanCuts()
    {
        Assert.Equal(3, ArenaRank.FromRating(400, 5).Division);
        Assert.Equal(2, ArenaRank.FromRating(700, 5).Division);
        Assert.Equal(1, ArenaRank.FromRating(900, 5).Division);
    }

    [Fact]
    public void PlacementKeepsMedalHidden()
    {
        var display = ArenaRank.FromRating(1600, 4);
        Assert.Equal(ArenaLeague.Unranked, display.League);
        Assert.Equal("Unranked · 1600", display.Label);
    }

    [Fact]
    public void ApplyRunSkipsMirrorOnlyRuns()
    {
        var delta = ArenaRank.ApplyRun(800, 0, 10, hadRealOpponent: false);
        Assert.False(delta.Applied);
        Assert.Equal(0, delta.Delta);
        Assert.Equal(800, delta.RatingAfter);
    }

    [Fact]
    public void TenWinFromStartIsABigClimb()
    {
        var delta = ArenaRank.ApplyRun(800, 0, 10, hadRealOpponent: true);
        Assert.True(delta.Applied);
        Assert.Equal(160, delta.Delta);
        Assert.Equal(960, delta.RatingAfter);
        Assert.Equal(1, delta.After.RatedRuns);
        Assert.Equal(ArenaLeague.Unranked, delta.After.League);
    }

    [Fact]
    public void FourWinsFromStartIsEven()
    {
        var delta = ArenaRank.ApplyRun(800, 0, 4, hadRealOpponent: true);
        Assert.Equal(0, delta.Delta);
    }

    [Fact]
    public void FifthRatedRunRevealsMedal()
    {
        var delta = ArenaRank.ApplyRun(960, 4, 7, hadRealOpponent: true);
        Assert.True(delta.Applied);
        Assert.Equal(5, delta.After.RatedRuns);
        Assert.NotEqual(ArenaLeague.Unranked, delta.After.League);
    }

    [Fact]
    public void RatingCannotGoBelowZero()
    {
        var delta = ArenaRank.ApplyRun(10, 5, 0, hadRealOpponent: true);
        Assert.Equal(0, delta.RatingAfter);
    }

    [Fact]
    public void LegendLabelIncludesNumber()
    {
        Assert.Equal("Legend #3 · 1900", ArenaRank.FromRating(1900, 8, 3).Label);
    }

    [Fact]
    public void IsMirrorDetectsPrefix()
    {
        Assert.True(ArenaRank.IsMirrorPlayerId("mirror:abc"));
        Assert.False(ArenaRank.IsMirrorPlayerId("abc"));
    }
}
