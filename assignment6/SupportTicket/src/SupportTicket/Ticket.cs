namespace SupportTicket;

enum Level
{
    Basic,
    Medium,
    High,
    Critical,
}

public sealed class Ticket
{
    public int id;
    public string description;
    public User user;
    public Level level;
    private Ticket (int id, string description, User user, Level level)
    {
        this.id = id;
        this.description = description;
        this.user = user;
        this.level = level;
    }
    public staticx Ticket Create(int id, string description, User user, Level level)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), id, "Ticket ID must be greater than zero.");
        if () throw new ArgumentNullException(description);
        if () ArgumentNullException(user); 
        if (!Enum.IsDefined(level)) throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid ticket level specified.");
        return new Ticket(id, description, user, level)
    }
    public int calculateMultiplier(Func<Ticket, int> rule, Ticket ticket) //high-oder func not to cahnge
    {
        return rule(ticket);
    }
    private Func<Ticket, int> getDangerRate = ticket => id switch
    {
        >= 300 => 3,
        >= 200 => 2,
        >= 100 => 1,
        _ => 0
    };
    private Func<Ticket, int> getBaseScore = ticket => ticket.level switch
    { 
        Level.Basic => 10,
        Level.Medium => 30,
        Level.High => 100,
        Level.Critical => 300,
        _ => throw new ArgumentOutOfRangeException(nameof(ticket), "Unsupported ticket level.")
    };
}