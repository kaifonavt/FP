namespace SupportTicket;

enum Level
{
    // TODO: Add more levels (Batyr)
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

    public int calculateMultiplier(Func<Ticket, int> rule, Ticket ticket) //high-oder func not to cahnge
    {
        return rule(ticket);
    }

    public Ticket () {
        // TODO: Constructor (Meir)
    }


    private Func<Ticket, int> getDangerRate = ticket =>
    {
        ArgumentNullException.ThrowIfNull(ticket);

        string? description = ticket.description;

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Ticket description cannot be empty.",
                nameof(ticket));
        }

        return description switch
        {
            var d when d.Contains("500") => 5,
            var d when d.Contains("404") => 3,
            var d when d.Contains("400") => 2,
            _ => 1
        };
    };

    private Func<Ticket, int> getBaseScore = ticket =>
    {
        ArgumentNullException.ThrowIfNull(ticket);

        int baseScore = ticket.level switch
        {
            Level.Basic => 10,
            Level.Medium => 25,
            Level.High => 50,
            Level.Critical => 100,
            _ => throw new ArgumentOutOfRangeException(
                nameof(ticket),
                "Unsupported ticket level.")
        };

        return baseScore *
               ticket.calculateMultiplier(ticket.getDangerRate, ticket);
    };

}