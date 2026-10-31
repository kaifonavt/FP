using Xunit;
using static BuildingSecurity.Tests.TestData;

namespace BuildingSecurity.Tests;

public class ExtensionTests
{
    [Fact]
    public void ActiveOnly_ExcludesCompletedVisits()
    {
        var active = Ok(Visit.Create("P1", 101, Now.AddHours(-1), Option<DateTimeOffset>.None));
        var completed = Ok(active.Exit(Now));
        Assert.Same(active, Assert.Single(new[] { completed, active }.ActiveOnly()));
    }

    [Fact]
    public void WithDuration_IsDeferred_AndSnapshotDoesNotRepeatProcessing()
    {
        int processed = 0;
        IEnumerable<Visit> Source()
        {
            processed++;
            yield return Ok(Visit.Create("P1", 101, Now.AddMinutes(-60.5), Option<DateTimeOffset>.None));
        }
        var query = Source().WithDuration(Now);
        Assert.Equal(0, processed);
        Assert.Equal(60, Assert.Single(query).Minutes);
        Assert.Equal(1, processed);
        var snapshot = query.ToArray();
        Assert.Equal(2, processed);
        Assert.Equal(60, Assert.Single(snapshot).Minutes);
        Assert.Single(snapshot);
        Assert.Equal(2, processed);
    }

    [Fact]
    public void WithDuration_UsesExitForCompletedVisit()
    {
        var visit = Ok(Visit.Create("P1", 101, Now.AddHours(-2), Option<DateTimeOffset>.Some(Now.AddHours(-1))));
        Assert.Equal(60, Assert.Single(new[] { visit }.WithDuration(Now)).Minutes);
    }

    [Theory]
    [InlineData(60, 0, 60)]
    [InlineData(90, 30, 100)]
    [InlineData(30, 0, 0)]
    public void CurriedFunctions_ScoreSameInputDifferently(long minutes, int dayExpected, int nightExpected)
    {
        var day = RiskScoring.CreateScore(Ok(ScorePolicy.Create(60, 1)));
        var night = RiskScoring.CreateScore(Ok(ScorePolicy.Create(30, 2)));
        Assert.Equal(dayExpected, day(minutes));
        Assert.Equal(nightExpected, night(minutes));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(60, 0)]
    [InlineData(60, 101)]
    public void InvalidPolicy_IsResultFailure(int threshold, int weight) =>
        Error(ScorePolicy.Create(threshold, weight), "InvalidPolicy");
}
