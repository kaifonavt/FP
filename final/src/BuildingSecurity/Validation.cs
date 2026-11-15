namespace BuildingSecurity;

public static class Validation
{
    public static bool ValidId(string? id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 128 &&
        id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');
    private static bool Level(int value) => value is >= 1 and <= 5;
    public static Result<SecurityConfig> Config(SecurityConfig config) =>
        Enum.IsDefined(config.Mode) && Level(config.LockdownMinLevel) && config.OpenTime >= TimeSpan.Zero &&
        config.OpenTime < TimeSpan.FromDays(1) && config.CloseTime >= TimeSpan.Zero &&
        config.CloseTime < TimeSpan.FromDays(1) && config.OpenTime != config.CloseTime
        ? Result<SecurityConfig>.Success(config)
        : Result<SecurityConfig>.Failure(ErrorCode.InvalidInput, "Invalid mode, clearance level, or working hours");

    public static Result<SecurityState> State(SecurityState s)
    {
        Result<SecurityState> Bad(string message) => Result<SecurityState>.Failure(ErrorCode.InvalidInput, message);
        if (s.Employees.IsDefault || s.Cards.IsDefault || s.Rooms.IsDefault || s.Tickets.IsDefault || s.Reservations.IsDefault || s.Audit.IsDefault || s.Config is null)
            return Bad("A required collection or configuration is missing");
        if (!Records(s.Employees, e => e.Id) || !Records(s.Cards, c => c.Id) || !Records(s.Rooms, r => r.Id) ||
            !Records(s.Tickets, t => t.Id) || !Records(s.Reservations, r => r.Id) || !Records(s.Audit, a => a.Id))
            return Bad("Invalid or duplicate IDs, or null records");
        if (s.Employees.Any(e => string.IsNullOrWhiteSpace(e.Name) || string.IsNullOrWhiteSpace(e.Department) || !Level(e.ClearanceLevel)) ||
            s.Rooms.Any(r => string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Type) || !Level(r.RequiredLevel)))
            return Bad("Invalid names, departments, room types, or clearance levels (1–5)");
        var employees = s.Employees.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var rooms = s.Rooms.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        if (s.Cards.Any(c => !employees.Contains(c.EmployeeId)) || s.Tickets.Any(t => !employees.Contains(t.EmployeeId)) ||
            s.Reservations.Any(r => !employees.Contains(r.EmployeeId) || !rooms.Contains(r.RoomId)))
            return Bad("A record refers to a missing employee or room");
        if (s.Tickets.Any(t => string.IsNullOrWhiteSpace(t.Description) || t.CreatedAt == default || !Enum.IsDefined(t.Status) ||
            (t.Status == TicketStatus.Open ? t.ResolvedAt is not null : t.ResolvedAt is null || t.ResolvedAt < t.CreatedAt)))
            return Bad("Invalid ticket status or timestamp");
        if (s.Reservations.Any(r => !Enum.IsDefined(r.Status) || r.Start >= r.End)) return Bad("Invalid reservation time range or status");
        if (s.Reservations.Any(r => r.Status == ReservationStatus.Active && s.Reservations.Any(other => other.Id != r.Id && Reservations.Overlaps(r.RoomId, r.Start, r.End)(other))))
            return Bad("Stored active reservations overlap");
        if (s.Audit.Any(a => !ValidId(a.CardId) || !ValidId(a.RoomId) || string.IsNullOrWhiteSpace(a.Reason) || a.Timestamp == default ||
            (a.EmployeeId is not null && !employees.Contains(a.EmployeeId)) ||
            (a.Granted && (a.EmployeeId is null || !rooms.Contains(a.RoomId) || !s.Cards.Any(c => c.Id == a.CardId && c.EmployeeId == a.EmployeeId)))))
            return Bad("Invalid audit entry");
        return Config(s.Config).Map(_ => s);
    }

    private static bool Records<T>(IEnumerable<T> records, Func<T, string> id) where T : class
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return records.All(record => record is not null && ValidId(id(record)) && seen.Add(id(record)));
    }
}
