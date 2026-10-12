using System;
using System.Collections.Generic;
using System.Linq;

// FP MIDTERM PRACTICE - IT-2512 - VARIANT 2 - STUDENT STARTER
// Room bookings | 4 tasks | 60 minutes | 6 + 8 + 10 + 16 = 40 points
// Each task is independent. Complete STUDENT CODE and WRITTEN ANSWER Q1-Q4.
// Marking: code 4 + 6 + 8 + 14 = 32; written answers 2 + 2 + 2 + 2 = 8.
// Write 2-4 English sentences per answer. Use this file in a separate console project.

public enum RoomKind { Regular, Computer, Lab }
public record Booking(int Id, string Room, int Hours, bool IsConfirmed, RoomKind Kind);

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
    // Count valid records that satisfy either rule: Computer records have Hours less than or equal to maxHours;
    // Lab records have Hours greater than or equal to minHours. Regular records never qualify.
    // Ignore invalid lines and continue processing. Return 0 for empty input or no matches.
    // Assume all numeric parameters are in [2, 10].
    // Input format: Kind;Hours. TryReadLine is provided and correct.
    // >>> STUDENT CODE 1A >>>
    // Find and fix exactly TWO logical errors. Keep the foreach loop.
    public static int CountAcceptedBookings(IEnumerable<string> lines, int minHours, int maxHours)
    {
        int count = 0;
        foreach (string line in lines)
        {
            if (!TryReadLine(line, out string kind, out int value))
                continue;

            if ((kind == "Computer" && value >= minHours) || (kind == "Lab" && value <= maxHours)) count++;
        }
        return count;
    }
    // <<< END STUDENT CODE 1A <<<

    private static void DemoTask1()
    {
        // Provided example; this does NOT count as either student diagnostic check.
        string[] sample = new[] { "Computer;2", "Computer;7", "Computer;3", "Lab;6", "Lab;2", "Regular;8" };
        Console.WriteLine("Expected: 3");
        Console.WriteLine("Actual:   " + CountAcceptedBookings(sample, 6, 3));
        // >>> STUDENT CODE 1B >>>
        // TODO: Add two Check(label, expectedInt, actualInt) calls.
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
        if (kind != "Regular" && kind != "Computer" && kind != "Lab") return false;
        return int.TryParse(parts[1].Trim(), out value)
            && value >= 2 && value <= 10;
    }

    // >>> WRITTEN ANSWER Q1 | 2 POINTS >>>
    // Question: With minHours = 6 and maxHours = 3, explain why Computer;2 qualifies while Lab;2 does not.
    // Which numeric rule belongs to each category?
    // YOUR ANSWER: Replace this line with 2-4 English sentences.
    // <<< END WRITTEN ANSWER Q1 <<<

    // TASK 2 | 6 code + 2 written = 8 points
    // Evaluate must calculate rule(item) exactly once. If condition(item) is true, pass the score to transform;
    // otherwise return it unchanged. Do not call transform when the condition is false.
    // BaseScore: switch on Kind; Regular=5, Computer=10, Lab=16; then add Hours * 2.
    // Assume valid records and non-null functions as described in the task document.
    public static int Evaluate(Booking item, Func<Booking, int> rule, Func<Booking, bool> condition, Func<int, int> transform)
    {
        // >>> STUDENT CODE 2A >>>
        // TODO: Implement Evaluate according to the task description
        int score = rule(item);
        if (condition(item))
        {
            score = transform(score);
        }
        return score;
        // <<< END STUDENT CODE 2A <<<
    }

    public static int BaseScore(Booking item)
    {
        // >>> STUDENT CODE 2B >>>
        // TODO: Use a switch expression and the required numeric addition.
        int value = item.Kind switch
        {
            RoomKind.Regular => 5,
            RoomKind.Computer => 10,
            RoomKind.Lab => 16
        };
        return value + (item.Hours * 2);
        // <<< END STUDENT CODE 2B <<<
    }

    private static void DemoTask2()
    {
        var sample = new Booking(99, "Sample", 3, true, RoomKind.Lab);
        
        Console.WriteLine("Expected scores: 26 and 22");
        // >>> STUDENT CODE 2C >>>
        Console.WriteLine("Actual: " + Evaluate(sample, BaseScore, x => x.Hours >= 3, score => score + 4));
        Console.WriteLine("Actual: " + Evaluate(sample, BaseScore, x => x.Hours > 5, score => score * 2));
        // <<< END STUDENT CODE 2C <<<
    }

    // >>> WRITTEN ANSWER Q2 | 2 POINTS >>>
    // Question: Suppose rule(item) returns 20 and condition(item) returns false. What should Evaluate return,
    // and should transform be called? Explain what happens when transform is x => x * 3.
    // YOUR ANSWER: Replace this line with 2-4 English sentences.
    // <<< END WRITTEN ANSWER Q2 <<<

    // TASK 3 | 8 code + 2 written = 10 points
    // For each pair of neighbouring values, calculate their sum. Return that result only if it is less than or
    // equal to maxSum. Use yield return, one traversal and fixed extra storage. No LINQ, indexing,
    // Count(), repeated traversal, temporary list/array or shared state.
    public static IEnumerable<int> AdjacentSums(IEnumerable<int> values, int maxSum)
    {
        // >>> STUDENT CODE 3 >>>
        // TODO: Write the lazy iterator.
        using var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext()) yield break;
        int previous = enumerator.Current;
        while (enumerator.MoveNext())
        {
            int current = enumerator.Current;
            if (previous+current <= maxSum)
            {
                yield return previous + current;
            }

            previous = current;
        }
        // <<< END STUDENT CODE 3 <<<
    }

    private static void DemoTask3()
    {
        ShowSequence("4, 1, 6, 2, 8", new[] { 4, 1, 6, 2, 8 }, 7, new[] { 5, 7 });
        ShowSequence("2, 5, 1", new[] { 2, 5, 1 }, 5, Array.Empty<int>());
        ShowSequence("-3, 5, -1, 4", new[] { -3, 5, -1, 4 }, 3, new[] { 2, 3 });
        ShowSequence("7", new[] { 7 }, 5, Array.Empty<int>());
        ShowSequence("<empty>", Array.Empty<int>(), 5, Array.Empty<int>());
    }

    // >>> WRITTEN ANSWER Q3 | 2 POINTS >>>
    // Question: If the result of a neighbouring pair fails the filter, which source value must become previous
    // for the next pair? Explain why updating previous only when a result is returned gives wrong pairs.
    // YOUR ANSWER: Replace this line with 2-4 English sentences.
    // <<< END WRITTEN ANSWER Q3 <<<

    // TASK 4 | 14 code + 2 written = 16 points
    // Filter: IsConfirmed is true; Hours <= maxHours; Kind is not Regular.
    // Use rule once per eligible record. Sort by score descending, then Id ascending.
    // Return at most count strings as "Id: Room [score]". Use LINQ.
    public static string[] BuildReport(IEnumerable<Booking> items, Func<Booking, int> rule, int maxHours, int count)
    {
        // >>> STUDENT CODE 4 >>>
        // TODO: Build the LINQ report. Independent of Tasks 2 and 3.
        return items
            .Where(x => x.IsConfirmed && x.Hours <= maxHours && x.Kind != RoomKind.Regular)
            .Select(x => new { Booking = x, Score = rule(x) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Booking.Id)
            .Take(count)
            .Select(x => $"{x.Booking.Id}: {x.Booking.Room} [{x.Score}]")
            .ToArray();
        // <<< END STUDENT CODE 4 <<<
    }

    private static Booking[] SampleItems() => new[]
    {
        new Booking(9, "Lab A", 4, true, RoomKind.Lab),
        new Booking(4, "Lab B", 4, true, RoomKind.Lab),
        new Booking(2, "Room C", 4, true, RoomKind.Regular),
        new Booking(1, "Room D", 6, true, RoomKind.Lab),
        new Booking(7, "Room E", 4, false, RoomKind.Lab),
        new Booking(6, "Room F", 3, true, RoomKind.Computer)
    };

    private static void DemoTask4()
    {
        Func<Booking, int> rule = x => x.Hours * 2 + (x.Kind == RoomKind.Lab ? 3 : 0);
        string[] expected = { "4: Lab B [11]", "9: Lab A [11]", "6: Room F [6]" };
        string[] actual = BuildReport(SampleItems(), rule, 4, 3);
        Console.WriteLine("Expected: " + string.Join(" | ", expected));
        Console.WriteLine("Actual:   " + string.Join(" | ", actual));
        CheckSequence("Report example", expected, actual);
    }

    // >>> WRITTEN ANSWER Q4 | 2 POINTS >>>
    // Question: Why must the Kind filter be applied before Take(count)? Why does the final projection reuse Score
    // instead of calling rule again?
    // YOUR ANSWER: Replace this line with 2-4 English sentences.
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
