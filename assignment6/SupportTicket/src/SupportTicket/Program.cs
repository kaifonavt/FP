namespace SupportTicket;

class Program
{ 
    static void Main()
    {
        List<User> validUsers;
        validUsers = UserParser.ParseUsersFromFile("users.json");
        Console.WriteLine("=== Create Support Ticket ===");
        Console.Write("Enter User ID: ");
        if (!int.TryParse(Console.ReadLine(), out int userId))
        {
            Console.WriteLine("Invalid User ID format.");
            return;
        }
        if (!User.IsValid(userId, validUsers))
        {
            Console.WriteLine("Error: User ID does not exist in authorized users record.");
            return;
        }
        User user = validUsers.FirstOrDefault(u => u.id == userId);
        Console.Write("Enter Ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int ticketId))
        {
            Console.WriteLine("Invalid Ticket ID format.");
            return;
        }
        Console.Write("Enter Ticket Description: ");
        string description = Console.ReadLine();
        Console.WriteLine("Select Ticket Level (0 = Basic, 1 = Medium, 2 = High, 3 = Critical): ");
        if (!int.TryParse(Console.ReadLine(), out int levelValue) || !Enum.IsDefined(typeof(Level), levelValue))
        {
            Console.WriteLine("Invalid Ticket Level choice.");
            return;
        }
        Level level = (Level)levelValue;
        TicketResult result = Ticket.Create(ticketId, description, user, level);
        switch (result)
        {
            case ValidTicket validResult:
                int totalScore = Ticket.Evaluate(validResult.Ticket);
                Console.WriteLine($"{ticketId}|{description}|{user.name} {user.surname}|{totalScore}");
                break;
            case InvalidTicket invalidResult:
                Console.WriteLine($"Error creating ticket: {invalidResult.Error}");
                break;
        }
    }
}