#!/usr/bin/env dotnet
public enum DeliveryMode
{ Standard, Express, Priority }

public record Delivery(
    int Id, string Destination, int Weight,
    bool IsReady, DeliveryMode Mode);

public class Program
{
    static int SumExpressWeights(IEnumerable<string> lines)
    {
        int total = 0;
        foreach (string line in lines)
        {
            if (!TryParseLine(line, out string mode, out int weight)) continue;
            if (mode == "Express") total += weight;
        } 
        return total;
    }

    public static int Evaluate(
        Delivery item, Func<Delivery, int> rule) => rule(item);

    public static int BaseScore(Delivery item) => item.Mode switch
    {
        DeliveryMode.Standard => 5,
        DeliveryMode.Express  => 10,
        DeliveryMode.Priority => 20,
        
    };

    public static IEnumerable<int> AdjacentSums(IEnumerable<int> values)
    {
        var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext()) yield break;
        int previous = enumerator.Current;
        while (enumerator.MoveNext())
        {
            int current = enumerator.Current;
            yield return previous + current;
            previous = current;
        }
    }

    public void Main()
    {
        Console.WriteLine();

    }
    public void DemoTask2()
    {
        Check    
    }

}