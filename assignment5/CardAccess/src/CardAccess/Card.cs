using System;
using System.Text.Json.Serialization;

namespace CardAccess;

public sealed class Card
{
    public string id { get; }
    public int userId { get; }
    public bool active { get; }

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
        this.id = rule?.Invoke(userId) ?? GetHexId(userId);

        if (string.IsNullOrWhiteSpace(this.id))
            throw new ArgumentException("ID must be non-empty.", nameof(id));
    }

    public string GetHashId(Func<int, string> rule)
    {
        return rule(this.userId);
    }

    public static string GetHexId(int number)
    {
        ulong hash1 = 14695981039346656037UL;
        ulong hash2 = 1099511628211UL;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(number.ToString());
        foreach (byte b in bytes)
        {
            hash1 = (hash1 ^ b) * 1099511628211UL;
            hash2 = (hash2 * 14695981039346656037UL) ^ b;
        }
        ulong result = ((hash1 & 0xFFFFFFFF) << 32) | (hash2 & 0xFFFFFFFF);
        return result.ToString("x16");
    }
}