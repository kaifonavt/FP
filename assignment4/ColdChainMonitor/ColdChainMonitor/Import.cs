using System.Text;

namespace ColdChainMonitor;

public static partial class Pipeline
{
    public static ImportResult ReadReadings(Stream input)
    {
        using var reader = new StreamReader(input, Encoding.UTF8, true, 4096, leaveOpen: true);
        var accepted = new List<Reading>();
        var rejected = new List<ImportError>();
        var sensorTypes = new Dictionary<string, StorageClass>(StringComparer.OrdinalIgnoreCase);
        for (int row = 1; ; row++)
        {
            string? text = reader.ReadLine();
            if (text is null) break;
            if (string.IsNullOrWhiteSpace(text)) continue;
            bool valid = TryParseReading(text, out var item, out var message);
            if (valid && sensorTypes.TryGetValue(item!.SensorId, out var existing)
                && existing != item.StorageClass)
            { valid = false; message = "A sensor cannot switch its storage class."; }
            if (valid)
            {
                accepted.Add(item!);
                sensorTypes[item!.SensorId] = item.StorageClass;
            }
            else rejected.Add(new ImportError(row, text, message));
        }
        return new ImportResult(accepted.ToArray(), rejected.ToArray());
    }
}
