public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

public readonly struct SupportTicket
{
    public int Id { get; }
    public TicketPriority Priority { get; }
    public string Title { get; }
    public int EstimatedHours { get; }

    public SupportTicket(int id, TicketPriority priority, string title, int estimatedHours)
    {
        Id = id;
        Priority = priority;
        Title = title ?? string.Empty;
        EstimatedHours = estimatedHours;
    }

    public override string ToString()
    {
        return $"[#{Id}] {Priority} | {Title} ({EstimatedHours}h)";
    }
}

internal class Program
{
    private static readonly Func<SupportTicket, int> strategy = ticket => ticket.Priority switch
    {
        TicketPriority.Critical => 100,
        TicketPriority.High => 60,
        TicketPriority.Medium => 30,
        TicketPriority.Low => 10,
        _ => 0
    };

    private static int CalculateScore(
        SupportTicket ticket,
        Func<SupportTicket, int> strategy)
    {
        return strategy(ticket);
    }

    private static (bool Success, SupportTicket? Value, string Error) ParseTicket(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (false, null, "Empty Input failure.");
        var parts = text.Split('|')
            .Select(p => p.Trim())
            .ToArray();
        if (parts.Length < 4) return (false, null, "Invalid Ticket format");

        if (!int.TryParse(parts[0], out var id) || id <= 0) return (false, null, "Invalid ID failure");

        if (!Enum.TryParse<TicketPriority>(parts[1], true, out var priority) || !Enum.IsDefined(priority))
            return (false, null, "Invalid priority failure");

        if (string.IsNullOrWhiteSpace(parts[2])) return (false, null, "Empty title failure");
        var title = parts[2];
        if (!int.TryParse(parts[3], out var hours) || hours < 1 || hours > 8)
            return (false, null, "Invalid time failure");

        return (true, new SupportTicket(id, priority, title, hours), string.Empty);
    }

    private static IEnumerable<(bool Success, SupportTicket? Value, string Error)> ParseTickets(
        IEnumerable<string> lines)
    {
        foreach (var line in lines) yield return ParseTicket(line);
    }

    private static int PriorityFirstStrategy(SupportTicket ticket)
    {
        return CalculateScore(ticket, strategy);
    }

    private static int QuickFixStrategy(SupportTicket ticket)
    {
        return CalculateScore(ticket, strategy) + (ticket.EstimatedHours == 1 ? 20 : 0);
    }

    private static string GetAction(SupportTicket ticket)
    {
        return ticket switch
        {
            { Priority: TicketPriority.Critical } => "Escalate immediately",
            { Priority: TicketPriority.High, EstimatedHours: >= 4 } => "Assign senior specialist",
            { Priority: TicketPriority.High, EstimatedHours: < 4 } => "Prioritize",
            { Priority: TicketPriority.Medium } => "Standard queue",
            { Priority: TicketPriority.Low } => "Low priority queue",
            _ => "Unknown action"
        };
    }

    private static bool IsUrgent(SupportTicket ticket)
    {
        return ticket.Priority == TicketPriority.Critical || ticket.Priority == TicketPriority.High;
    }

    private static int CalculateTotalHours(IEnumerable<SupportTicket> tickets)
    {
        var total = 0;
        foreach (var ticket in tickets)
            total += ticket.EstimatedHours;
        return total;
    }

    private static void PrintSummary(List<SupportTicket> validTickets)
    {
        var urgentCount = validTickets.Count(IsUrgent);
        var totalHours = CalculateTotalHours(validTickets);

        Console.WriteLine("\nSUMMARY REPORT");
        Console.WriteLine($"VALID TICKETS: {validTickets.Count}");
        Console.WriteLine($"URGENT VALID TICKETS: {urgentCount}");
        Console.WriteLine($"TOTAL ESTIMATED HOURS: {totalHours}");
    }

    private static void Main(string[] args)
    {
        Console.WriteLine("FunctionalSupportDesk");
        Console.WriteLine("id|priority|title|estimatedHours");
        string[] rawTickets =
        {
            "101|High|Cannot sign in|2",
            "102|Low|Change profile photo|1",
            "bad|Critical|Payment service unavailable|4",
            "104|Unknown|Printer problem|2",
            "105|Medium||3",
            "106|Critical|Database unavailable|5"
        };
        var validTickets = new List<SupportTicket>();
        var validCount = 0;
        var invalidCount = 0;

        foreach (var result in ParseTickets(rawTickets))
            if (result.Success && result.Value.HasValue)
            {
                validCount++;
                var ticket = result.Value.Value;
                validTickets.Add(ticket);

                var action = GetAction(ticket);
                var scorePriority = CalculateScore(ticket, PriorityFirstStrategy);
                var scoreQuickFix = CalculateScore(ticket, QuickFixStrategy);

                Console.WriteLine($"[VALID] {ticket}");
                Console.WriteLine(
                    $"Action: {action} | PriorityScore: {scorePriority} | QuickFixScore: {scoreQuickFix}");
            }
            else
            {
                invalidCount++;
                Console.WriteLine($"[INVALID] Error: {result.Error}");
            }

        Console.WriteLine($"\nVALID TICKETS: {validCount}");
        Console.WriteLine($"INVALID TICKETS: {invalidCount}");

        var summaryThread = new Thread(() => PrintSummary(validTickets));
        summaryThread.Start();
        summaryThread.Join();
    }
}