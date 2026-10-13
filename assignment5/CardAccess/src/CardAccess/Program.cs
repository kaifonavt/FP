namespace CardAccess;

public enum AccessLevel
{
    Guest = 0, 
    Staff = 1, 
    Admin = 2
}
public class Program
{ 
    // TODO: Get User by comparing Id
    public User? GetUser(User[] users, Card card)
    {
        foreach (var user in users)
        {
            if (user.id == card.userId) return user;
        }
        return null;
    }
    public bool Validate(Card card, Room room, User user) => (card.active) && (room.level == user.level);
    static void Main()
    {
        Console.WriteLine("Welcome to SEC");
        Console.WriteLine("Input Card UUID");
    }
}

