namespace BuildingSecurity;

public sealed class OpenTicket
{
    public Ticket Value { get; }
    private OpenTicket(Ticket value) => Value = value;
    public static Result<OpenTicket> From(Ticket ticket) => ticket switch
    {
        { Status: TicketStatus.Open, ResolvedAt: null } => Result<OpenTicket>.Success(new(ticket)),
        _ => Result<OpenTicket>.Failure(ErrorCode.TicketAlreadyResolved, "Ticket is already closed")
    };
    public Result<ResolvedTicket> Resolve(DateTimeOffset now) => now < Value.CreatedAt
        ? Result<ResolvedTicket>.Failure(ErrorCode.InvalidInput, "Resolution time cannot precede creation time")
        : Result<ResolvedTicket>.Success(new(Value with { Status = TicketStatus.Closed, ResolvedAt = now }));
}
public sealed class ResolvedTicket
{
    public Ticket Value { get; }
    internal ResolvedTicket(Ticket value) => Value = value;
}

public static class Tickets
{
    public static Result<SecurityState> Create(SecurityState state, string employeeId, string description, DateTimeOffset now, string id)
    {
        if (string.IsNullOrWhiteSpace(description) || !Validation.ValidId(id) || state.Tickets.Any(t => t.Id == id))
            return Result<SecurityState>.Failure(ErrorCode.InvalidInput, "A description and a unique ticket ID are required");
        return state.Employees.FindEntity(e => e.Id == employeeId, $"employee {employeeId}")
            .Map(_ => state with { Tickets = state.Tickets.Add(new(id, employeeId, description.Trim(), now, TicketStatus.Open, null)) });
    }
    public static Result<SecurityState> Resolve(SecurityState state, string id, DateTimeOffset now) =>
        state.Tickets.FindEntity(t => t.Id == id, $"ticket {id}")
            .Bind(OpenTicket.From).Bind(open => open.Resolve(now))
            .Map(closed => state with { Tickets = state.Tickets.Select(t => t.Id == id ? closed.Value : t).ToImmutableArray() });
}