using System.Text;
using System.Text.Json.Serialization;

namespace CardAccess;

public sealed class Card
{
    [JsonConstructor]
    public Card(string id, int userId, bool active)
    {
        this.userId = userId;
        this.active = active;
        this.id = string.IsNullOrWhiteSpace(id) ? GetHexId(userId) : id;
    }

    public Card(Func<int, string> rule, int userId, bool active)
    {
        this.userId = userId;
        this.active = active;
        id = rule?.Invoke(userId) ?? GetHexId(userId);

        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID must be non-empty.", nameof(id));
    }

    public string id { get; }
    public int userId { get; }
    public bool active { get; }

    public string GetHashId(Func<int, string> rule)
    {
        return rule(userId);
    }

    public static string GetHexId(int number)
    {
        var hash1 = 14695981039346656037UL;
        var hash2 = 1099511628211UL;
        var bytes = Encoding.UTF8.GetBytes(number.ToString());
        foreach (var b in bytes)
        {
            hash1 = (hash1 ^ b) * 1099511628211UL;
            hash2 = (hash2 * 14695981039346656037UL) ^ b;
        }

        var result = ((hash1 & 0xFFFFFFFF) << 32) | (hash2 & 0xFFFFFFFF);
        return result.ToString("x16");
    }
}