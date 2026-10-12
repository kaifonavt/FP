using System;
using System.Collections.Generic;
using System.Linq;

// FP MIDTERM PRACTICE - IT-2511 - VARIANT 3 - STUDENT STARTER
// Support tickets | 4 tasks | 60 minutes | 6 + 8 + 10 + 16 = 40 points
// Each task is independent. Complete STUDENT CODE and WRITTEN ANSWER Q1-Q4.
// Marking: code 4 + 6 + 8 + 14 = 32; written answers 2 + 2 + 2 + 2 = 8.
// Write 2-4 English sentences per answer. Use this file in a separate console project.

public enum TicketPriority { Low, Medium, High }
public record SupportTicket(int Id, string Title, int Age, bool IsOpen, TicketPriority Priority);

public static class Program
{
    private static int passed;
    private static int failed;

    public static void Main()
    {
        RunTask("Task 1 - Debug batch processing - 4 code + 2 written points", DemoTask1);
        RunTask("Task 2 - Higher-order scoring - 6 code + 2 written points", DemoTask2);
        RunTask("Task 3 - Lazy adjacent values - 8 code + 2 written points", DemoTask3);
        RunTask("Task 4 - LINQ report - 14 code + 2 written points", DemoTask4);
    }

    // TASK 1 | 4 code + 2 written = 6 points
    // Count valid records whose Priority is Medium or High and whose Age is between minAge and maxAge inclusive. Ignore Low records. Ignore invalid lines and continue processing. Return 0 for empty input or no matches. Assume all numeric parameters are in [1, 10] and minAge <= maxAge.
    // Input format: Priority;Age. TryReadLine is provided and correct.
    // >>> STUDENT CODE 1A >>>
    // Find and fix exactly TWO logical errors. Keep the foreach loop.
    public static int CountAcceptedTickets(IEnumerable<string> lines, int minAge, int maxAge)
    {
        int count = 0;
        foreach (string line in lines)
        {
            if (!TryReadLine(line, out string kind, out int value)) continue;
            if (kind != "Low" && value >= minAge && value <= maxAge) count++;
        }
        return count;
    }
    // <<< END STUDENT CODE 1A <<<

    private static void DemoTask1()
    {
        // Provided example; this does NOT count as either student diagnostic check.
        string[] sample = new[] { "Medium;2", "Medium;6", "High;3", "High;5", "Low;4", "Medium;4" };
        Console.WriteLine("Expected: 3");
        Console.WriteLine("Actual:   " + CountAcceptedTickets(sample, 3, 5));
        // >>> STUDENT CODE 1B >>>
        // TODO: Add two Check(label, expectedInt, actualInt) calls.
        string[] sample1 = new[] { "Medium;2", "Medium;6", "Low;3", "Low;5", "Low;4", "Medium;4" };
        string[] sample2 = new[] { "Medium;3", "Medium;3", "High;3", "High;3", "Low;3", "Medium;3" };
        Check("Acceptability of High Medium", 2, CountAcceptedTickets(sample1, 3, 5));
        Check("Exclusivity of minAge", 5, CountAcceptedTickets(sample1, 3, 6));
        // Each input must expose a different bug and fail before the fixes.
        // <<< END STUDENT CODE 1B <<<
    }

    private static bool TryReadLine(string line, out string kind, out int value)
    {
        kind = "";
        value = 0;
        if (string.IsNullOrWhiteSpace(line)) return false;
        string[] parts = line.Split(';');
        if (parts.Length != 2) return false;
        kind = parts[0].Trim();
        if (kind != "Low" && kind != "Medium" && kind != "High") return false;
        return int.TryParse(parts[1].Trim(), out value)
            && value >= 1 && value <= 10;
    }

    // >>> WRITTEN ANSWER Q1 | 2 POINTS >>>
    // Question: Why must the Age range apply to both Medium and High? With the range 3..5, give one line exposing the
    // grouping error and one line exposing the inclusive boundary error.
    // YOUR ANSWER: Becouse they are part of one logical statement 
    //Inculusive error related to '<' instead of '<='
    // <<< END WRITTEN ANSWER Q1 <<<

    // TASK 2 | 6 code + 2 written = 8 points
    // Evaluate must calculate scoreRule(item) exactly once, pass that score to adjustment exactly once, and return the adjusted result.
    // BaseScore: switch on Priority; Low=4, Medium=9, High=15; then add Age * 2.
    // Assume valid records and non-null functions as described in the task document.
    public static int Evaluate(SupportTicket item, Func<SupportTicket, int> scoreRule, Func<int, int> adjustment)
    {
        // >>> STUDENT CODE 2A >>>
        // TODO: Implement Evaluate according to the task description.
        return adjustment(scoreRule(item));
        // <<< END STUDENT CODE 2A <<<
    }

    public static int BaseScore(SupportTicket item)
    {
        // >>> STUDENT CODE 2B >>>
        int score = item.Priority switch 
        {
            TicketPriority.Low => 4,
            TicketPriority.Medium => 9,
            TicketPriority.High => 15
        };
        return score + item.Age * 2;
        // <<< END STUDENT CODE 2B <<<
    }

    private static void DemoTask2()
    {
        var sample = new SupportTicket(99, "Sample", 3, true, TicketPriority.High);
        Console.WriteLine("Expected scores: 19 and 42");
        // >>> STUDENT CODE 2C >>>
        // TODO: Make these two Evaluate calls and print both returned values:
        Console.WriteLine(Evaluate(sample, BaseScore, score => score - 2));
        Console.WriteLine(Evaluate(sample, BaseScore, score => score * 2));
        // <<< END STUDENT CODE 2C <<<
    }

    // >>> WRITTEN ANSWER Q2 | 2 POINTS >>>
    // Question: Suppose scoreRule(item) returns 18. What does Evaluate(item, scoreRule, x => x - 4) return?
    // Explain the order of the two function calls and whether the method stays pure if adjustment changes global state.
    // YOUR ANSWER: 14 
    // firstly scoreRule then adjustment, its pure because no mutability, calculations devided.
    // <<< END WRITTEN ANSWER Q2 <<<

    // TASK 3 | 8 code + 2 written = 10 points
    // For each pair of neighbouring values, calculate their sum. Return that result only if it is greater than or equal to minSum.
    // Use yield return, one traversal and fixed extra storage. No LINQ, indexing,
    // Count(), repeated traversal, temporary list/array or shared state.
    public static IEnumerable<int> AdjacentSums(IEnumerable<int> values, int minSum)
    {
        // >>> STUDENT CODE 3 >>>
        // TODO: Write the lazy iterator.
        using var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext()) yield break;
        int previous = enumerator.Current;
        while (enumerator.MoveNext())
        {
            int current = enumerator.Current;
            if (previous + current >= minSum) yield return previous + current;
            previous = current;
        }
        // <<< END STUDENT CODE 3 <<<
    }

    private static void DemoTask3()
    {
        ShowSequence("4, 1, 6, 2, 8", new[] { 4, 1, 6, 2, 8 }, 7, new[] { 7, 8, 10 });
        ShowSequence("2, 5, 1", new[] { 2, 5, 1 }, 5, new[] { 7, 6 });
        ShowSequence("-3, 5, -1, 4", new[] { -3, 5, -1, 4 }, 3, new[] { 4, 3 });
        ShowSequence("7", new[] { 7 }, 5, Array.Empty<int>());
        ShowSequence("<empty>", Array.Empty<int>(), 5, Array.Empty<int>());
    }

    // >>> WRITTEN ANSWER Q3 | 2 POINTS >>>
    // Question: If the result of a neighbouring pair fails the filter, which source value must become previous for
    // the next pair? Explain why updating previous only when a result is returned gives wrong pairs.
    // YOUR ANSWER: to move to next
    // becouse if return after it will be sum of itself
    // <<< END WRITTEN ANSWER Q3 <<<

    // TASK 4 | 14 code + 2 written = 16 points
    // Filter: IsOpen is true; Age >= minAge; Priority is not Low.
    // Use rule once per eligible record. Sort by score descending, then Id descending.
    // Return at most count strings as "Id: Title [score]". Use LINQ.
    public static string[] BuildReport(IEnumerable<SupportTicket> items, Func<SupportTicket, int> rule, int minAge, int count)
    {
        // >>> STUDENT CODE 4 >>>
        // TODO: Build the LINQ report. Independent of Tasks 2 and 3.
        return items
            .Where(x => x.IsOpen && x.Age >= minAge && x.Priority!=TicketPriority.Low)
            .Select(x => new { SupportTicket = x, Score = rule(x) })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.SupportTicket.Id)
            .Take(count)
            .Select(x => $"{x.SupportTicket.Id}: {x.SupportTicket.Title} [{x.Score}]")
            .ToArray();
        // <<< END STUDENT CODE 4 <<<
    }

    private static SupportTicket[] SampleItems() => new[]
    {
        new SupportTicket(9, "Login", 4, true, TicketPriority.High),
        new SupportTicket(4, "Payment", 4, true, TicketPriority.High),
        new SupportTicket(2, "Printer", 4, true, TicketPriority.Low),
        new SupportTicket(1, "Network", 3, true, TicketPriority.High),
        new SupportTicket(7, "Account", 4, false, TicketPriority.High),
        new SupportTicket(6, "Email", 6, true, TicketPriority.Medium)
    };

    private static void DemoTask4()
    {
        Func<SupportTicket, int> rule = x => x.Age * 3 + (x.Priority == TicketPriority.High ? 4 : 0);
        string[] expected = { "6: Email [18]", "9: Login [16]", "4: Payment [16]" };
        string[] actual = BuildReport(SampleItems(), rule, 4, 3);
        Console.WriteLine("Expected: " + string.Join(" | ", expected));
        Console.WriteLine("Actual:   " + string.Join(" | ", actual));
        CheckSequence("Report example", expected, actual);
    }

    // >>> WRITTEN ANSWER Q4 | 2 POINTS >>>
    // Question: Why must the Priority filter be applied before Take(count)? Why is ThenByDescending used for equal
    // scores instead of a second OrderByDescending?
    // YOUR ANSWER: becouse we will take not the lowest count 
    // because we nned secondary order filtration
    // <<< END WRITTEN ANSWER Q4 <<<

    // PROVIDED HELPERS
    private static void ShowSequence(string input, int[] values, int parameter, int[] expected)
    {
        Console.WriteLine("Input: " + input);
        int[] actual = AdjacentSums(values, parameter).ToArray();
        Console.WriteLine("Expected: " + Format(expected));
        Console.WriteLine("Actual:   " + Format(actual));
        CheckSequence("Adjacent example", expected, actual);
    }

    private static string Format(int[] values) =>
        values.Length == 0 ? "<empty>" : string.Join(", ", values);

    private static void Check<T>(string label, T expected, T actual)
    {
        bool ok = EqualityComparer<T>.Default.Equals(expected, actual);
        if (ok) passed++; else failed++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}: {label}");
    }

    private static void CheckSequence<T>(string label, IEnumerable<T> expected, IEnumerable<T> actual)
    {
        Check(label, true, expected.SequenceEqual(actual));
    }

    private static void RunTask(string name, Action action)
    {
        Console.WriteLine();
        Console.WriteLine(name);
        try { action(); }
        catch (NotImplementedException ex) { Console.WriteLine("TODO: " + ex.Message); }
        catch (Exception ex)
        {
            failed++;
            Environment.ExitCode = 1;
            Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
