namespace BuildingSecurity;

public sealed record AccessSummaryRow(string EmployeeId, string EmployeeName, string Department, int Attempts, int Granted, int Denied);
public sealed record RoomUtilizationRow(string RoomId, string RoomName, int Sessions, double TotalHours, double AverageHours);
public sealed record AnomalyRow(string Subject, int Denied);

public static class Reports
{
    public static Func<string?, Func<bool, Func<AuditEntry, bool>>> AuditFilter(string? employeeId) =>
        roomId => deniedOnly => entry =>
            (employeeId is null || entry.EmployeeId == employeeId) &&
            (roomId is null || entry.RoomId == roomId) && (!deniedOnly || !entry.Granted);

    public static AccessSummaryRow[] AccessSummary(SecurityState state)
    {
        var events = state.Audit
            .Where(entry => entry.EmployeeId is not null)
            .Join(state.Employees,
                entry => entry.EmployeeId,
                employee => employee.Id,
                (entry, employee) => new { Entry = entry, Employee = employee });

        return events
            .GroupBy(item => item.Employee)
            .Select(group => new AccessSummaryRow(
                group.Key.Id,
                group.Key.Name,
                group.Key.Department,
                group.Count(),
                group.Count(item => item.Entry.Granted),
                group.Count(item => !item.Entry.Granted)))
            .OrderByDescending(row => row.Denied)
            .ThenBy(row => row.EmployeeName, StringComparer.Ordinal)
            .ThenBy(row => row.EmployeeId, StringComparer.Ordinal)
            .ToArray();
    }

    public static RoomUtilizationRow[] RoomUtilization(SecurityState state) => state.Reservations
        .Where(r => r.Status == ReservationStatus.Active).GroupBy(r => r.RoomId)
        .Select(g => new RoomUtilizationRow(g.Key, state.Rooms.First(r => r.Id == g.Key).Name,
            g.Count(), g.Sum(r => (r.End - r.Start).TotalHours), g.Average(r => (r.End - r.Start).TotalHours)))
        .OrderByDescending(r => r.TotalHours).ThenBy(r => r.RoomId, StringComparer.Ordinal).ToArray();

    public static AnomalyRow[] Anomalies(IEnumerable<AuditEntry> entries, int threshold) => entries
        .Where(a => !a.Granted).GroupBy(a => a.EmployeeId is { } e ? $"employee:{e}" : $"card:{a.CardId}")
        .Select(g => new AnomalyRow(g.Key, g.Count())).Where(r => r.Denied >= threshold)
        .OrderByDescending(r => r.Denied).ThenBy(r => r.Subject, StringComparer.Ordinal).ToArray();
}
