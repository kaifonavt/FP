namespace BuildingSecurity;

public sealed class Guest
{
    private Guest(int id, string name, bool isBlocked)
    {
        (Id, Name, IsBlocked) = (id, name, isBlocked);
    }

    public int Id { get; }
    public string Name { get; }
    public bool IsBlocked { get; }

    public static Result<Guest, AppError> Create(
        int id,
        string? name,
        bool isBlocked)
    {
        return id <= 0 || string.IsNullOrWhiteSpace(name)
            ? Result<Guest, AppError>.Failure(
                new AppError("InvalidGuest",
                    $"Guest {id}: positive ID and name are required."))
            : Result<Guest, AppError>.Success(
                new Guest(id, name.Trim(), isBlocked));
    }
}