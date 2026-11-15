namespace BuildingSecurity;

public sealed record UnverifiedScan(string CardId, string RoomId, DateTimeOffset Timestamp);
public sealed record ScanEntities(Card Card, Employee Employee, Room Room);

// The private constructor makes successful validation the only way to obtain this type.
public sealed class VerifiedCard
{
    public Card Card { get; }
    public Employee Owner { get; }
    private VerifiedCard(Card card, Employee owner) { Card = card; Owner = owner; }
    public static Result<VerifiedCard> Create(Card card, Employee employee) => (card, employee) switch
    {
        ({ IsActive: true }, { IsActive: true }) when card.EmployeeId == employee.Id =>
            Result<VerifiedCard>.Success(new(card, employee)),
        _ => Result<VerifiedCard>.Failure(ErrorCode.InactiveAccountOrCard, "Card or employee is inactive, or the card belongs to another employee")
    };
}
public sealed record VerifiedScan(VerifiedCard Subject, Room Room);
public sealed record AccessDecision(VerifiedCard Subject, Room TargetRoom, bool Granted, string Reason);
public sealed record ScanOutcome(SecurityState State, Result<AccessDecision> Decision);
public delegate Result<bool> AccessPolicy(Employee employee, Room room, TimeSpan time);

public static class Access
{
    public static Result<ScanEntities> LookupEntities(SecurityState state, UnverifiedScan scan) =>
        state.Cards.FindEntity(c => c.Id == scan.CardId, $"card {scan.CardId}")
        .Bind(card => state.Employees.FindEntity(e => e.Id == card.EmployeeId, $"employee {card.EmployeeId}")
        .Bind(employee => state.Rooms.FindEntity(r => r.Id == scan.RoomId, $"room {scan.RoomId}")
        .Map(room => new ScanEntities(card, employee, room))));

    public static Result<VerifiedScan> VerifyCardAndEmployee(ScanEntities entities) =>
        VerifiedCard.Create(entities.Card, entities.Employee).Map(card => new VerifiedScan(card, entities.Room));

    public static AccessPolicy SelectPolicy(SecurityConfig config) => (employee, room, time) =>
        (config.Mode, employee.ClearanceLevel >= room.RequiredLevel) switch
        {
            (_, false) => Result<bool>.Failure(ErrorCode.InsufficientClearance, "Insufficient clearance level"),
            (BuildingMode.Normal, _) when !IsWithinHours(config, time) =>
                Result<bool>.Failure(ErrorCode.AccessDenied, "Building is closed: outside working hours"),
            (BuildingMode.Night, _) when employee.ClearanceLevel < 3 =>
                Result<bool>.Failure(ErrorCode.AccessDenied, "Night mode: clearance level 3 required"),
            (BuildingMode.Lockdown, _) when employee.ClearanceLevel < config.LockdownMinLevel =>
                Result<bool>.Failure(ErrorCode.AccessDenied, $"Lockdown: clearance level {config.LockdownMinLevel} required"),
            (BuildingMode.Normal or BuildingMode.Night or BuildingMode.Lockdown, _) => Result<bool>.Success(true),
            _ => Result<bool>.Failure(ErrorCode.InvalidInput, "Unknown mode")
        };

    // Currying; fixing policy and time yields a partially applied rule for this scan.
    public static Func<TimeSpan, Func<Room, Func<VerifiedCard, Result<AccessDecision>>>> CheckAccess(AccessPolicy policy) =>
        time => room => verified => policy(verified.Owner, room, time).Bind(allowed => allowed
            ? Result<AccessDecision>.Success(new(verified, room, true, "Access granted"))
            : Result<AccessDecision>.Failure(ErrorCode.AccessDenied, "Access denied by policy"));

    public static Result<AccessDecision> EvaluateAccessPolicy(VerifiedScan scan, TimeSpan time, AccessPolicy policy) =>
        CheckAccess(policy)(time)(scan.Room)(scan.Subject);

    public static bool IsWithinHours(SecurityConfig config, TimeSpan time) => config.OpenTime < config.CloseTime
        ? time >= config.OpenTime && time < config.CloseTime
        : time >= config.OpenTime || time < config.CloseTime;

    public static ScanOutcome Scan(SecurityState state, string cardId, string roomId, DateTimeOffset now,
        string auditId, AccessPolicy? policy = null)
    {
        var scan = new UnverifiedScan(cardId, roomId, now);
        var decision = LookupEntities(state, scan)
            .Bind(VerifyCardAndEmployee)
            .Bind(verified => EvaluateAccessPolicy(verified, now.TimeOfDay, policy ?? SelectPolicy(state.Config)));
        return AppendAuditLog(state, scan, auditId, decision);
    }

    public static ScanOutcome AppendAuditLog(SecurityState state, UnverifiedScan scan, string id, Result<AccessDecision> result)
    {
        var employeeId = state.Cards.FindMaybe(c => c.Id == scan.CardId) switch
        {
            Maybe<Card>.Some(var card) => card.EmployeeId,
            _ => null
        };
        var entry = result.Match(
            d => new AuditEntry(id, scan.Timestamp, scan.CardId, employeeId, scan.RoomId, true, d.Reason),
            e => new AuditEntry(id, scan.Timestamp, scan.CardId, employeeId, scan.RoomId, false, $"{e.Code}: {e.Message}"));
        return new(state with { Audit = state.Audit.Add(entry) }, result);
    }
}
