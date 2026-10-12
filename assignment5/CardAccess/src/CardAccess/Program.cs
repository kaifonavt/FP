namespace CardAccess;
public class Program
{
    // TODO: Get User by comparing Id
    public User? GetUser(User[] users, Card card)
    {
        for (int i = 0; i < users.Length; i++)
        {
                if (users[i].id == card.userId) return users[i];
        }
        return null;
    }
    public bool Validate(Card card, Room room, User user) => (card.active) && (room.Type == user.level);
    static void Main()
    {
        Console.WriteLine("Welcome to SEC");
        Console.WriteLine("Input Card UUID");
    }
}

