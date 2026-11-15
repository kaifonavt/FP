namespace BuildingSecurity;

public static class Reservations
{
    public static Func<Reservation, bool> Overlaps(string roomId, DateTimeOffset start, DateTimeOffset end) =>
        r => r is { Status: ReservationStatus.Active } && r.RoomId == roomId && start < r.End && r.Start < end;

    public static Result<SecurityState> Book(SecurityState state, string roomId, string employeeId,
        DateTimeOffset start, DateTimeOffset end, string id)
    {
        if (!Validation.ValidId(id) || state.Reservations.Any(r => r.Id == id) || start >= end)
            return Result<SecurityState>.Failure(ErrorCode.InvalidInput, "A unique ID and a time range with start < end are required");
        return state.Employees.FindEntity(e => e.Id == employeeId, $"employee {employeeId}")
            .Bind(employee => state.Rooms.FindEntity(r => r.Id == roomId, $"room {roomId}")
            .Bind(room => ValidateBooking(state, employee, room, start, end))
            .Map(_ => state with { Reservations = state.Reservations.Add(new(id, roomId, employeeId, start, end, ReservationStatus.Active)) }));
    }

    private static Result<bool> ValidateBooking(SecurityState state, Employee employee, Room room, DateTimeOffset start, DateTimeOffset end) =>
        (employee.IsActive, employee.ClearanceLevel >= room.RequiredLevel, state.Reservations.Any(Overlaps(room.Id, start, end))) switch
        {
            (false, _, _) => Result<bool>.Failure(ErrorCode.InactiveAccountOrCard, "Employee is inactive"),
            (_, false, _) => Result<bool>.Failure(ErrorCode.InsufficientClearance, "Insufficient clearance level"),
            (_, _, true) => Result<bool>.Failure(ErrorCode.TimeSlotConflict, "Room is already booked for this time"),
            _ => Result<bool>.Success(true)
        };

    public static Result<SecurityState> Cancel(SecurityState state, string id) =>
        state.Reservations.FindEntity(r => r.Id == id, $"reservation {id}").Bind(r => r.Status switch
        {
            ReservationStatus.Cancelled => Result<SecurityState>.Failure(ErrorCode.AlreadyCancelled, "Reservation is already cancelled"),
            _ => Result<SecurityState>.Success(state with
            { Reservations = state.Reservations.Replace(r, r with { Status = ReservationStatus.Cancelled }) })
        });
}
