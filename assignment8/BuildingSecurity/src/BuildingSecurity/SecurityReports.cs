namespace BuildingSecurity;

public sealed record VisitRow(string PassId, int GuestId, string GuestName, int HostId,
    string HostName, int Capacity, int RoomId, long Minutes);
public sealed record AttentionRow(string PassId, int GuestId, string GuestName, int RoomId,
    long Minutes, int DayScore, int NightScore);
public sealed record HostTotal(int HostId, string HostName, int Capacity, int ActiveGuests, long TotalMinutes);
public sealed record HostLoadRow(int HostId, string HostName, int Capacity, int ActiveGuests,
    long TotalMinutes, int LoadPercent);
public sealed record ReportBundle(DateTimeOffset AsOf, IReadOnlyList<AttentionRow> Attention,
    IReadOnlyList<HostLoadRow> HostLoad);

public static class SecurityReports
{
    public static Result<IReadOnlyList<VisitRow>, AppError> Prepare(SecurityData state, DateTimeOffset asOf)
    {
        if (state.Visits.Any(v => v.EnteredAt > asOf || v.ExitedAt.Match(exit => exit > asOf, () => false)))
            return Result<IReadOnlyList<VisitRow>, AppError>.Failure(new("InvalidReportTime", "Report time must not precede any recorded entry or exit."));

        var passes = state.Passes.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var guests = state.Guests.ToDictionary(g => g.Id);
        var hosts = state.Employees.ToDictionary(e => e.Id);
        var query = state.Visits.ActiveOnly().WithDuration(asOf).Select(timed =>
        {
            var pass = passes[timed.Visit.PassId];
            var host = hosts[pass.EmployeeId];
            return new VisitRow(pass.Id, pass.GuestId, guests[pass.GuestId].Name,
                host.Id, host.Name, host.MaxGuests, timed.Visit.RoomId, timed.Minutes);
        });
        return Result<IReadOnlyList<VisitRow>, AppError>.Success(Array.AsReadOnly(query.ToArray()));
    }

    public static Result<IReadOnlyList<AttentionRow>, AppError> Attention(
        IEnumerable<VisitRow> rows, long minimumMinutes, Func<long, int> day, Func<long, int> night)
    {
        if (minimumMinutes < 0)
            return Result<IReadOnlyList<AttentionRow>, AppError>.Failure(new("InvalidFilter", "Minimum minutes must be nonnegative."));
        var report = rows.Where(row => row.Minutes >= minimumMinutes)
            .Select(row => new AttentionRow(row.PassId, row.GuestId, row.GuestName, row.RoomId,
                row.Minutes, day(row.Minutes), night(row.Minutes)))
            .OrderByDescending(row => row.NightScore)
            .ThenBy(row => row.PassId, StringComparer.Ordinal).ToArray();
        return Result<IReadOnlyList<AttentionRow>, AppError>.Success(Array.AsReadOnly(report));
    }

    public static IReadOnlyList<HostTotal> HostTotals(IEnumerable<VisitRow> rows) =>
        Array.AsReadOnly(rows.GroupBy(row => new { row.HostId, row.HostName, row.Capacity })
            .Select(group => new HostTotal(group.Key.HostId, group.Key.HostName, group.Key.Capacity,
                group.Count(), group.Sum(row => row.Minutes)))
            .OrderByDescending(row => row.ActiveGuests)
            .ThenByDescending(row => row.TotalMinutes)
            .ThenBy(row => row.HostId).ToArray());
}
