
namespace ColdChainMonitor;

public static partial class Pipeline
{
    private static bool Outside(Reading r) => r.StorageClass switch
    {
        StorageClass.Cold => r.Temperature < 2m || r.Temperature > 8m,
        StorageClass.Frozen => r.Temperature < -22m || r.Temperature > -15m,
        _ => throw new ArgumentOutOfRangeException(nameof(r))
    };

    private static bool Jump(decimal previous, decimal current)
    {
        try { return Math.Abs(current - previous) > 4m; }
        catch (OverflowException) { return true; }
    }

    private static Alert MakeAlert(Reading r, bool abrupt) =>
        new(r.SensorId, r.Timestamp, r.Temperature, Outside(r), abrupt);

    private static Alert[] Ordered(IEnumerable<Alert> alerts) => alerts
        .OrderBy(a => a.SensorId, StringComparer.OrdinalIgnoreCase)
        .ThenBy(a => a.Timestamp).ToArray();
    public static Alert[] Analyze(IEnumerable<Reading> readings)
    {
        var found = new List<Alert>();
        var groups = readings.GroupBy(r => r.SensorId, StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            var timeline = group.OrderBy(r => r.Timestamp).ToList();
            for (int i = 0; i < timeline.Count; i++)
            {
                Reading current = timeline[i];
                bool changed = i > 0 && Jump(timeline[i - 1].Temperature, current.Temperature);
                if (Outside(current) || changed) found.Add(MakeAlert(current, changed));
            }
        }
        return Ordered(found);
    }
}
