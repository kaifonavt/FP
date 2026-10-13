using System;
using System.Text.Json.Serialization;

namespace CardAccess;

public class User
{
    public int id { get; }
    public string name { get; }

    [JsonPropertyName("surname")]
    public string surename { get; }

    public AccessLevel level { get; }

    [JsonConstructor]
    public User(int id, string name, string surename, AccessLevel level)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Field name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(surename))
            throw new ArgumentException("Field surename cannot be empty.", nameof(surename));

        this.id = id;
        this.name = name.Trim();
        this.surename = surename.Trim();
        this.level = level;
    }
}