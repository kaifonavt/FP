namespace CardAccess;

public record AccessRequest(int UserId, string CardId, int RoomNumber);
public record EnrichedRequest(User User, Card Card, Room Room);
public record AccessResult(string UserName, int RoomNumber, bool AccessGranted, string Reason);

public enum AccessLevel
{
    Guest = 0,
    Staff = 1,
    Admin = 2
}

public class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("System Card Access Control");

        var users = Parser.ReadList<User>("users.json");
        var cards = Parser.ReadList<Card>("cards.json");
        var rooms = Parser.ReadList<Room>("rooms.json");

        while (true)
        {

            Console.Write("Enter card UUID (or 'exit' to quit): ");
            string? cardId = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(cardId)) continue;
            if (cardId.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
            
            var card = cards.FirstOrDefault(c => c.id == cardId);
            if (card == null)
            {
                Console.WriteLine("[Error] Card with this UUID was not found in the system.");
                continue;
            }

            Console.Write("Enter room number for access: ");
            string? roomInput = Console.ReadLine();
    
            if (!int.TryParse(roomInput, out int roomNumber))
            {
                Console.WriteLine("[Error] Invalid room number format.");
                continue;
            }
            
            var requests = new List<AccessRequest>
            {
                new AccessRequest(card.userId, card.id, roomNumber)
            };

            Console.WriteLine("\n[1] Strict Policy Evaluation:");
            var strictResults = AccessPipeline.ProcessWorkflow(requests, users, cards, rooms, AccessPipeline.StrictPolicy).ToList();
            PrintResults(strictResults);

            Console.WriteLine("\n[2] Hierarchical Policy Evaluation:");
            var hierResults = AccessPipeline.ProcessWorkflow(requests, users, cards, rooms, AccessPipeline.HierarchicalPolicy).ToList();
            PrintResults(hierResults);
        }
        Console.WriteLine("System shutdown complete.");
    }
    static void PrintResults(IEnumerable<AccessResult> results)
    {
        if (!results.Any())
        {
            Console.WriteLine("  -> Denied: User or room not found.");
            return;
        }

        foreach (var res in results)
        {
            string status = res.AccessGranted ? "GRANTED" : "DENIED";
            Console.WriteLine($"  -> User: {res.UserName} | Room: {res.RoomNumber} | Access: {status} | Reason: {res.Reason}");
        }
    }
}