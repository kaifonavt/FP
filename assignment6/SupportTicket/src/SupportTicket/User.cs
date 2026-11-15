using System.Text.Json.Serialization;

namespace SupportTicket;

public sealed class User
{
    [JsonConstructor]
    private User(int id, string name, string surname)
    {
        this.id = id;
        this.name = name;
        this.surname = surname;
    }

    public int id { get; }
    public string name { get; }
    public string surname { get; }

    public static User Create(int id, string? name, string? surname)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "User ID must be greater than zero.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Surname cannot be empty.", nameof(surname));
        return new User(id, name.Trim(), surname.Trim());
    }

    public static bool IsValid(int id, IEnumerable<User> users)
    {
        foreach (var user in users)
            if (id == user.id)
                return true;
        return false;
    }
}