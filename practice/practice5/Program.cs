#!/usr/bin/env dotnet

internal enum TicketKind
{
    Question,
    Request,
    Incedent
}

public class Program
{
    //Return a bonus for a TicketKind: Question gives 5, Request gives 10, and Incident gives 20. Use a switch expression. For
    //any undeclared enum value, throw ArgumentOutOfRangeException.
    private static int KindBonus(TicketKind kind)
    {
        switch (kind)
        {
            case TicketKind.Question: return 5;
            case TicketKind.Request: return 10;
            case TicketKind.Incedent: return 20;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    //Implement Apply so that it invokes rule once with value and returns the result. In Main, use one lambda that adds 7 and
    //another that multiplies by 3. Do not hard-code either calculation inside Apply.
    private static int Apply(int value, Func<int, int> rule)
    {
        return rule(value);
    }

    //Apply first to the input and then apply second to that result. Return the final integer. Keep the method pure and invoke
    //each supplied function once.
    private int ApplyInOrder(int value, Func<int, int> first,
        Func<int, int> second)
    {
        return second(first(value));
    }

    //Use one foreach loop to count values accepted by the supplied predicate. Do not use LINQ. In Main, call the same
    //method once for positive values and once for even values.
    private int CountMatching(IEnumerable<int> values, Func<int, bool> test)
    {
        var count = 0;
        foreach (var value in values) count += test(value) ? 1 : 0;

        return count;
    }

    //The method must accept only integers from 1 to 6 inclusive. Null, blank, non-integer and out-of-range input must
    //return false. Repair the condition, retaining TryParse and the out parameter. The value output is meaningful only
    //when the method returns true.
    private bool TryRead(string text, out int value)
    {
        return int.TryParse(text, out value) && value >= 1 && value <= 6;
    }

    //Sum every string that can be parsed as an integer. Ignore invalid lines and continue. Return 0 for an empty input.
    //There are two logic errors in the method below. Repair both, then create one small diagnostic check for each error.
    private int SumValid(IEnumerable<string> lines)
    {
        var total = 0;
        foreach (var line in lines)
            if (int.TryParse(line, out var n))
                total += n;

        return total;
    }

    //Return the smallest strictly positive integer, or -1 if none exists. Diagnose and repair the state
    //initialization and update logic below. Use one pass and no LINQ.
    private int SmallestPositive(IEnumerable<int> values)
    {
        var best = -1;
        foreach (var n in values)
            if (n > 0 && (best == -1 || n < best))
                best = n;

        return best;
    }

    //Refactor this method so it returns a new array containing the original values followed by extra.
    //Accept IEnumerable<int> instead of List<int>. Keep the input unchanged and remove console output.
    //A local List<int> is allowed for this exercise
    int[] AppendValue(List<int> values, int extra){ 
        var list = new List<int>(values);
        list.Add(extra);
        return list.ToArray();
    }
    //Use yield return to produce every integer from start to end inclusive. If start is greater than end,
    //produce an empty sequence. Bounds are within −1000 to 1000. Do not build a collection or use LINQ.
    IEnumerable<int> InclusiveRange(int start, int end)
    {
        for (int n = start; n <= end; n++ ) yield return n ;
    }
    //Implement a lazy filter using foreach and yield return. Call test once for each inspected element and preserve
    //input order. Do not use Where, a temporary collection or repeated traversal.
    IEnumerable<int> Keep(IEnumerable<int> values, Func<int, bool> test)
    {
        foreach (int value in values)
        {
            if (test(value)) yield return value;
        }
    }
    //For each overlapping adjacent pair, yield current minus previous. Use one traversal, fixed local storage and
    //no LINQ, indexing or temporary collection. Empty and single-element inputs produce an empty sequence.
    IEnumerable<int> AdjacentChanges(IEnumerable<int> values)
    {
        using var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext()) yield break;
        int previous = enumerator.Current;
        while (enumerator.MoveNext())
        {
            int current = enumerator.Current;
            yield return current - previous;
            current = previous;
        }
    }
    //Yield the greater value from each overlapping neighbouring pair. This is a pairwise maximum, not the maximum
    //of the entire prefix. Use one pass, yield return and fixed local storage; do not use LINQ.
    IEnumerable<int> AdjacentMaximums(IEnumerable<int> values)
    {
        return null;
    }
    
    public void Main()
    {
        
    }
}