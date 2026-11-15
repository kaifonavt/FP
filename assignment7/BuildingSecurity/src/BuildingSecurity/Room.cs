namespace BuildingSecurity;

public sealed class Room
{
    private Room(int id, string name, int buildingId)
    {
        (Id, Name, BuildingId) = (id, name, buildingId);
    }

    public int Id { get; }
    public string Name { get; }
    public int BuildingId { get; }

    public static Result<Room, AppError> Create(
        int id,
        string? name,
        int buildingId)
    {
        return id <= 0 || buildingId <= 0 || string.IsNullOrWhiteSpace(name)
            ? Result<Room, AppError>.Failure(
                new AppError("InvalidRoom",
                    $"Room {id}: positive room/building IDs and a name are required."))
            : Result<Room, AppError>.Success(
                new Room(id, name.Trim(), buildingId));
    }
}