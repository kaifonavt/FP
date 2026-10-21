namespace BuildingSecurity;

public sealed class SecurityData
{
    public IReadOnlyList<Guest> Guests { get; }
    public IReadOnlyList<Employee> Employees { get; }
    public IReadOnlyList<Room> Rooms { get; }
    public IReadOnlyList<TemporaryPass> Passes { get; }
    public IReadOnlyList<Visit> Visits { get; }

    private SecurityData(
        IEnumerable<Guest> guests,
        IEnumerable<Employee> employees,
        IEnumerable<Room> rooms,
        IEnumerable<TemporaryPass> passes,
        IEnumerable<Visit> visits)
    {
        Guests = Array.AsReadOnly(guests.ToArray());
        Employees = Array.AsReadOnly(employees.ToArray());
        Rooms = Array.AsReadOnly(rooms.ToArray());
        Passes = Array.AsReadOnly(passes.ToArray());
        Visits = Array.AsReadOnly(visits.ToArray());
    }

    public static Result<SecurityData, AppError> Create(
        IEnumerable<Guest>? guests,
        IEnumerable<Employee>? employees,
        IEnumerable<Room>? rooms,
        IEnumerable<TemporaryPass>? passes,
        IEnumerable<Visit>? visits)
    {
        if (guests is null || employees is null || rooms is null
            || passes is null || visits is null)
        {
            return Fail(
                "InvalidData",
                "All five data collections must be supplied explicitly.");
        }

        var data = new SecurityData(
            guests, employees, rooms, passes, visits);

        if (data.Guests.Any(x => x is null)
            || data.Employees.Any(x => x is null)
            || data.Rooms.Any(x => x is null)
            || data.Passes.Any(x => x is null)
            || data.Visits.Any(x => x is null))
        {
            return Fail(
                "InvalidData",
                "Data collections cannot contain null records.");
        }

        if (HasDuplicates(data.Guests.Select(x => x.Id))
            || HasDuplicates(data.Employees.Select(x => x.Id))
            || HasDuplicates(data.Rooms.Select(x => x.Id))
            || HasDuplicates(data.Passes.Select(x => x.Id)))
        {
            return Fail(
                "DuplicateId",
                "Guest, employee, room and pass IDs must be unique within each collection.");
        }

        var guestIds = data.Guests.Select(x => x.Id).ToHashSet();
        var employeeIds = data.Employees.Select(x => x.Id).ToHashSet();
        var roomById = data.Rooms.ToDictionary(x => x.Id);
        var passById = data.Passes.ToDictionary(x => x.Id);

        foreach (var pass in data.Passes)
        {
            if (!guestIds.Contains(pass.GuestId)
                || !employeeIds.Contains(pass.EmployeeId))
            {
                return Fail(
                    "InvalidReference",
                    $"Pass '{pass.Id}' refers to an unknown guest or employee.");
            }

            foreach (var roomId in pass.AllowedRoomIds)
            {
                if (!roomById.TryGetValue(roomId, out var room)
                    || room.BuildingId != pass.BuildingId)
                {
                    return Fail(
                        "InvalidReference",
                        $"Pass '{pass.Id}' refers to unknown room {roomId} or another building.");
                }
            }
        }

        var activeGuests = new HashSet<int>();

        foreach (var visit in data.Visits)
        {
            if (!passById.TryGetValue(visit.PassId, out var pass))
            {
                return Fail(
                    "InvalidReference",
                    $"Visit refers to unknown pass '{visit.PassId}'.");
            }

            if (!pass.AllowedRoomIds.Contains(visit.RoomId)
                || visit.EnteredAt < pass.ValidFrom
                || visit.EnteredAt >= pass.ExpiresAt)
            {
                return Fail(
                    "InvalidVisit",
                    $"Visit for pass '{visit.PassId}' has an unauthorized zone or entry time.");
            }

            if (visit.IsActive && !activeGuests.Add(pass.GuestId))
            {
                return Fail(
                    "DuplicateActiveVisit",
                    $"Guest {pass.GuestId} has more than one active visit.");
            }
        }

        return Result<SecurityData, AppError>.Success(data);
    }

    private static bool HasDuplicates<T>(IEnumerable<T> ids) =>
        ids.GroupBy(x => x).Any(group => group.Count() > 1);

    private static Result<SecurityData, AppError> Fail(
        string code,
        string message) =>
        Result<SecurityData, AppError>.Failure(new(code, message));

    public Result<SecurityData, AppError> WithVisits(
        IEnumerable<Visit> visits) =>
        Create(Guests, Employees, Rooms, Passes, visits);
}