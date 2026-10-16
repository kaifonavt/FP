using System;

namespace SupportTicket;

public sealed class User
{
    public int Id { get; }
    public string Name { get; }
    public string Surname { get; }

    private User(int id, string name, string surname)
    {
        Id = id;
        Name = name;
        Surname = surname;
    }
    public static User Create(int id, string? name, string? surname)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "User ID must be greater than zero.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(surname)) throw new ArgumentException("Surname cannot be empty.", nameof(surname));
        return new User(id, name.Trim(), surname.Trim());
    }
}