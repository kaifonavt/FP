namespace SupportTicket;

public abstract record TicketResult;
public sealed record ValidTicket(Ticket Ticket) : TicketResult;
public sealed record InvalidTicket(string Error) : TicketResult;
public enum Level
{
    Basic,
    Medium,
    High,
    Critical,
}

public sealed class Ticket
{
    public int id { get; }
    public string description { get; }
    public User user { get; }
    public Level level { get; }
    private Ticket (int id, string description, User user, Level level)
    {
        this.id = id;
        this.description = description;
        this.user = user;
        this.level = level;
    }
    public static TicketResult Create(int id, string description, User user, Level level)
    {
        if (id <= 0) return new InvalidTicket("Ticket ID must be greater than zero.");
        if (string.IsNullOrWhiteSpace(description)) return new InvalidTicket("Description cannot be empty.");
        if (user == null) return new InvalidTicket("User cannot be null.");
        if (!Enum.IsDefined(typeof(Level), level)) return new InvalidTicket("Invalid ticket level specified.");
        return new ValidTicket(new Ticket(id, description, user, level));
    }
    private static int CalculateMultiplier(Func<Ticket, int> rule, Ticket ticket) //high-oder func not to cahnge
    {
        return rule(ticket);
    }
    private static Func<Ticket, int> getDangerRate = ticket => ticket.id switch
    {
        >= 300 => 3,
        >= 200 => 2,
        >= 100 => 1,
        _ => 0
    };
    private static Func<Ticket, int> getBaseScore = ticket => ticket.level switch
    { 
        Level.Basic => 10,
        Level.Medium => 30,
        Level.High => 100,
        Level.Critical => 300,
        _ => throw new ArgumentOutOfRangeException(nameof(ticket), "Unsupported ticket level.")
    };
    public static int Evaluate(Ticket ticket)
    {
        return CalculateMultiplier(getBaseScore, ticket) * CalculateMultiplier(getDangerRate, ticket);
    }
}