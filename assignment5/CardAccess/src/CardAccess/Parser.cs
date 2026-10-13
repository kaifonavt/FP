using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardAccess;

public static class Parser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static List<T> ReadList<T>(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"JSON file not found: {filePath}");

        string jsonString = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<T>>(jsonString, JsonOptions) ?? new List<T>();
    }
}