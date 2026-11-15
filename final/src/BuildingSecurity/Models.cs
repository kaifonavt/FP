using System.Collections.Immutable;

namespace BuildingSecurity;

public enum BuildingMode { Normal, Night, Lockdown }
public enum TicketStatus { Open, Closed }
public enum ReservationStatus { Active, Cancelled }
public sealed record Employee(string Id, string Name, string Department, int ClearanceLevel, bool IsActive);
public sealed record Card(string Id, string EmployeeId, bool IsActive);
public sealed record Room(string Id, string Name, string Type, int RequiredLevel);
public sealed record Ticket(string Id, string EmployeeId, string Description, DateTimeOffset CreatedAt,
    TicketStatus Status, DateTimeOffset? ResolvedAt);
public sealed record Reservation(string Id, string RoomId, string EmployeeId, DateTimeOffset Start,
    DateTimeOffset End, ReservationStatus Status);
public sealed record AuditEntry(string Id, DateTimeOffset Timestamp, string CardId, string? EmployeeId,
    string RoomId, bool Granted, string Reason);
public sealed record SecurityConfig(BuildingMode Mode, TimeSpan OpenTime, TimeSpan CloseTime, int LockdownMinLevel);
public sealed record SecurityState(ImmutableArray<Employee> Employees, ImmutableArray<Card> Cards,
    ImmutableArray<Room> Rooms, ImmutableArray<Ticket> Tickets, ImmutableArray<Reservation> Reservations,
    ImmutableArray<AuditEntry> Audit, SecurityConfig Config);
