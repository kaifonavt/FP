namespace BuildingSecurity;

public sealed class TemporaryPass
{
    public string Id { get; }
    public int GuestId { get; }
    public int EmployeeId { get; }
    public int BuildingId { get; }
    public DateTimeOffset ValidFrom { get; }
    public DateTimeOffset ExpiresAt { get; }
    public IReadOnlyList<int> AllowedRoomIds { get; }

    private TemporaryPass(
        string id,
        int guestId,
        int employeeId,
        int buildingId,
        DateTimeOffset validFrom,
        DateTimeOffset expiresAt,
        IEnumerable<int> allowedRoomIds)
    {
        Id = id;
        GuestId = guestId;
        EmployeeId = employeeId;
        BuildingId = buildingId;
        ValidFrom = validFrom;
        ExpiresAt = expiresAt;
        AllowedRoomIds = Array.AsReadOnly(allowedRoomIds.ToArray());
    }

    public static Result<TemporaryPass, AppError> Create(
        string? id,
        int guestId,
        int employeeId,
        int buildingId,
        DateTimeOffset validFrom,
        DateTimeOffset expiresAt,
        IEnumerable<int>? allowedRoomIds)
    {
        var rooms = allowedRoomIds?.ToArray();

        if (string.IsNullOrWhiteSpace(id)
            || guestId <= 0
            || employeeId <= 0
            || buildingId <= 0
            || expiresAt <= validFrom
            || rooms is null
            || rooms.Length == 0
            || rooms.Any(room => room <= 0)
            || rooms.Distinct().Count() != rooms.Length)
        {
            return Result<TemporaryPass, AppError>.Failure(
                new("InvalidPass",
                    $"Pass '{id}': IDs, a nonempty unique zone list and a positive validity interval are required."));
        }

        return Result<TemporaryPass, AppError>.Success( new TemporaryPass( 
            id.Trim(), 
            guestId,
            employeeId,
            buildingId,
            validFrom,
            expiresAt, 
            rooms)); }
}