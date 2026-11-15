using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ColdChainMonitor;

public static partial class Pipeline
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void WriteArchive(Stream output, MonitoringArchive archive)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(false), 1024, true);
        writer.Write(JsonSerializer.Serialize(archive, Options));
    }

    public static MonitoringArchive ReadArchive(Stream input)
    {
        using var reader = new StreamReader(input, Encoding.UTF8, true, 1024, true);
        return JsonSerializer.Deserialize<MonitoringArchive>(reader.ReadToEnd(), Options)
               ?? throw new JsonException("Expected an archive object, received null.");
    }

    public static bool SameValues(MonitoringArchive a, MonitoringArchive b)
    {
        return a.CreatedAtUtc == b.CreatedAtUtc && a.Readings.SequenceEqual(b.Readings)
                                                && a.Errors.SequenceEqual(b.Errors) && a.Alerts.SequenceEqual(b.Alerts);
    }
}