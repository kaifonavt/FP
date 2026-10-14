namespace SupportTicket;

enum Level
{
    // TODO: Add more levels (Batyr)
    Basic,
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

    private Func<Ticket, int> getDangerRate = ticket => ticket switch
    {
        // TODO: return multuplier according to error code table (Zhalgas)
    }; 
    private Func<Ticket , int> getBaseScore  = ticket => ticket switch 
    {
        //TODO: according to level return (Zhalgas)
        //return baseScore*
    };
}