namespace BuildingSecurity;

public static class ReportDisplay
{
    public static string Format(ReportBundle report)
    {
        var lines = new List<string> { $"Report time: {report.AsOf:O}", "Attention report:" };
        lines.AddRange(report.Attention.Select(row =>
            $"  {row.PassId}: {row.GuestName}, room={row.RoomId}, minutes={row.Minutes}, day={row.DayScore}, night={row.NightScore}"));
        if (report.Attention.Count == 0) lines.Add("  No matching active visits.");
        lines.Add("Host load report:");
        lines.AddRange(report.HostLoad.Select(row =>
            $"  {row.HostId}: {row.HostName}, active={row.ActiveGuests}/{row.Capacity}, minutes={row.TotalMinutes}, native load={row.LoadPercent}%"));
        if (report.HostLoad.Count == 0) lines.Add("  No active hosts.");
        return string.Join(Environment.NewLine, lines);
    }
}
