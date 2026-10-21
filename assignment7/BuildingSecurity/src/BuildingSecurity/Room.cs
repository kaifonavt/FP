namespace BuildingSecurity;

public sealed class Room
{
    public int Id { get; }
    public string Name { get; }
    public int BuildingId { get; }

    private Room(int id, string name, int buildingId) =>
        (Id, Name, BuildingId) = (id, name, buildingId);

    public static Result<Room, AppError> Create(
        int id,
        string? name,
        int buildingId) =>
        id <= 0 || buildingId <= 0 || string.IsNullOrWhiteSpace(name)
            ? Result<Room, AppError>.Failure(
                new("InvalidRoom",
                    $"Room {id}: positive room/building IDs and a name are required."))
            : Result<Room, AppError>.Success(
                new Room(id, name.Trim(), buildingId));
}