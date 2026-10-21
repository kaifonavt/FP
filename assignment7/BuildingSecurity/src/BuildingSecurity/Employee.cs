namespace BuildingSecurity;

public sealed class Employee
{
    public int Id { get; }
    public string Name { get; }
    public int MaxGuests { get; }

    private Employee(int id, string name, int maxGuests) =>
        (Id, Name, MaxGuests) = (id, name, maxGuests);

    public static Result<Employee, AppError> Create(
        int id,
        string? name,
        int maxGuests) =>
        id <= 0 || string.IsNullOrWhiteSpace(name) || maxGuests < 0
            ? Result<Employee, AppError>.Failure(
                new("InvalidEmployee",
                    $"Employee {id}: positive ID, name and nonnegative guest limit are required."))
            : Result<Employee, AppError>.Success(
                new Employee(id, name.Trim(), maxGuests));
}