using Xunit;
using static BuildingSecurity.Tests.TestData;

namespace BuildingSecurity.Tests;

public class ReportTests
{
    internal static SecurityData Sample() => Ok(Parser.Load(Path.Combine(AppContext.BaseDirectory, "reporting.json")));
    private static Func<long, int> Day => RiskScoring.CreateScore(Ok(ScorePolicy.Create(60, 1)));
    private static Func<long, int> Night => RiskScoring.CreateScore(Ok(ScorePolicy.Create(30, 2)));

    [Fact]
    public void Attention_FiltersCompleted_ProjectsScores_AndOrdersByRisk()
    {
        var rows = Ok(SecurityReports.Prepare(Sample(), Now));
        var report = Ok(SecurityReports.Attention(rows, 0, Day, Night));
        Assert.Equal(new[] { "P4", "P1" }, report.Select(x => x.PassId));
        Assert.Equal(new[] { 90L, 60L }, report.Select(x => x.Minutes));
        Assert.Equal(new[] { 30, 0 }, report.Select(x => x.DayScore));
        Assert.Equal(new[] { 100, 60 }, report.Select(x => x.NightScore));
    }

    [Fact]
    public void HostTotals_GroupsCountsAndSumsActiveVisits()
    {
        var report = SecurityReports.HostTotals(Ok(SecurityReports.Prepare(Sample(), Now)));
        Assert.Equal(new[] { 20, 10 }, report.Select(x => x.HostId));
        Assert.Equal(new[] { 1, 1 }, report.Select(x => x.ActiveGuests));
        Assert.Equal(new[] { 90L, 60L }, report.Select(x => x.TotalMinutes));
    }

    [Fact]
    public void EmptyDataset_AndNoMatches_ProduceEmptyReports()
    {
        var empty = Ok(SecurityData.Create(Array.Empty<Guest>(), Array.Empty<Employee>(), Array.Empty<Room>(), Array.Empty<TemporaryPass>(), Array.Empty<Visit>()));
        var rows = Ok(SecurityReports.Prepare(empty, Now));
        Assert.Empty(Ok(SecurityReports.Attention(rows, 0, Day, Night)));
        Assert.Empty(SecurityReports.HostTotals(rows));
        Assert.Empty(Ok(SecurityReports.Attention(Ok(SecurityReports.Prepare(Sample(), Now)), 1000, Day, Night)));
    }

    [Fact]
    public void TiedValues_OrderByPassIdAndHostIdRegardlessOfInputOrder()
    {
        var state = Sample();
        var visits = state.Visits.Where(x => x.IsActive)
            .Select(x => Ok(Visit.Create(x.PassId, x.RoomId, Now.AddHours(-1), Option<DateTimeOffset>.None))).Reverse();
        var rows = Ok(SecurityReports.Prepare(Ok(state.WithVisits(visits)), Now));
        Assert.Equal(new[] { "P1", "P4" }, Ok(SecurityReports.Attention(rows, 0, Day, Night)).Select(x => x.PassId));
        Assert.Equal(new[] { 10, 20 }, SecurityReports.HostTotals(rows).Select(x => x.HostId));
    }

    [Fact]
    public void MultipleVisitsForOneHost_AggregateWithoutCountingCompletedVisits()
    {
        var state = Sample();
        var additional = Ok(Visit.Create("P2", 101, Now.AddMinutes(-20), Option<DateTimeOffset>.None));
        var rows = Ok(SecurityReports.Prepare(Ok(state.WithVisits(state.Visits.Append(additional))), Now));
        var host = SecurityReports.HostTotals(rows).Single(x => x.HostId == 10);
        Assert.Equal(2, host.ActiveGuests);
        Assert.Equal(80, host.TotalMinutes);
    }

    [Fact]
    public void InvalidReportTimeAndFilter_ReturnExplicitErrors()
    {
        Error(SecurityReports.Prepare(Sample(), Now.AddDays(-1)), "InvalidReportTime");
        Error(SecurityReports.Attention(Ok(SecurityReports.Prepare(Sample(), Now)), -1, Day, Night), "InvalidFilter");
    }
}
