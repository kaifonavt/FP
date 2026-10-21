namespace BuildingSecurity;

public sealed record AccessRequest(string PassId, int RoomId);

public sealed record EnrichedRequest(
    SecurityData State,
    TemporaryPass Pass,
    Guest Guest,
    Employee Employee,
    Room Room,
    DateTimeOffset Now);

public sealed record AccessDecision(
    SecurityData NextState,
    string Message);

public static class AccessPipeline
{
    public static Option<T> Find<T>(
        IEnumerable<T> items,
        Func<T, bool> predicate) where T : notnull
    {
        foreach (var item in items)
        {
            if (predicate(item))
                return Option<T>.Some(item);
        }

        return Option<T>.None;
    }

    public static Result<EnrichedRequest, AppError> EnrichRequest(
        SecurityData state,
        AccessRequest request,
        DateTimeOffset now)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.PassId)
            || request.RoomId <= 0)
        {
            return Fail<EnrichedRequest>(
                "InvalidRequest",
                "A pass ID and positive room ID are required.");
        }

        return Find(state.Passes, p => p.Id == request.PassId.Trim())
            .ToResult(new AppError(
                "PassNotFound",
                $"Pass '{request.PassId}' was not found."))
            .Bind(pass => Find(state.Guests, g => g.Id == pass.GuestId)
                .ToResult(new AppError(
                    "GuestNotFound",
                    $"Guest {pass.GuestId} was not found."))
                .Bind(guest => Find(
                        state.Employees,
                        e => e.Id == pass.EmployeeId)
                    .ToResult(new AppError(
                        "EmployeeNotFound",
                        $"Employee {pass.EmployeeId} was not found."))
                    .Bind(employee => Find(
                            state.Rooms,
                            r => r.Id == request.RoomId)
                        .ToResult(new AppError(
                            "RoomNotFound",
                            $"Room {request.RoomId} was not found."))
                        .Map(room => new EnrichedRequest(
                            state, pass, guest, employee, room, now)))));
    }

    public static Result<EnrichedRequest, AppError> CheckGuestBlocked(
        EnrichedRequest request) =>
        request.Guest.IsBlocked
            ? Fail<EnrichedRequest>(
                "GuestBlocked",
                $"Guest {request.Guest.Id} ({request.Guest.Name}) is blacklisted.")
            : Result<EnrichedRequest, AppError>.Success(request);

    public static Result<EnrichedRequest, AppError> CheckPassValidity(
        EnrichedRequest request)
    {
        if (request.Now >= request.Pass.ExpiresAt)
        {
            return Fail<EnrichedRequest>(
                "PassExpired",
                $"Pass '{request.Pass.Id}' expired at {request.Pass.ExpiresAt:O}.");
        }

        if (request.Now < request.Pass.ValidFrom)
        {
            return Fail<EnrichedRequest>(
                "PassNotStarted",
                $"Pass '{request.Pass.Id}' is valid from {request.Pass.ValidFrom:O}.");
        }

        return Result<EnrichedRequest, AppError>.Success(request);
    }

    public static Result<EnrichedRequest, AppError> CheckZoneAccess(
        EnrichedRequest request) =>
        request.Room.BuildingId != request.Pass.BuildingId
        || !request.Pass.AllowedRoomIds.Contains(request.Room.Id)
            ? Fail<EnrichedRequest>(
                "ZoneAccessDenied",
                $"Pass '{request.Pass.Id}' does not allow room {request.Room.Id} in building {request.Room.BuildingId}.")
            : Result<EnrichedRequest, AppError>.Success(request);

    public static Result<EnrichedRequest, AppError> CheckGuestLimit(
        EnrichedRequest request)
    {
        var count = CountActiveGuests(
            request.State,
            request.Employee.Id);

        return count >= request.Employee.MaxGuests
            ? Fail<EnrichedRequest>(
                "HostGuestLimitExceeded",
                $"Employee {request.Employee.Id} ({request.Employee.Name}) already hosts {count}/{request.Employee.MaxGuests} guests.")
            : Result<EnrichedRequest, AppError>.Success(request);
    }

    public static int CountActiveGuests(
        SecurityData state,
        int employeeId)
    {
        var hostPassIds = state.Passes
            .Where(p => p.EmployeeId == employeeId)
            .Select(p => p.Id)
            .ToHashSet();

        return state.Visits.Count(
            visit => visit.IsActive && hostPassIds.Contains(visit.PassId));
    }

    private static Result<EnrichedRequest, AppError> CheckNotInside(
        EnrichedRequest request)
    {
        var guestPassIds = request.State.Passes
            .Where(p => p.GuestId == request.Guest.Id)
            .Select(p => p.Id)
            .ToHashSet();

        return request.State.Visits.Any(
            v => v.IsActive && guestPassIds.Contains(v.PassId))
            ? Fail<EnrichedRequest>(
                "AlreadyInside",
                $"Guest {request.Guest.Id} already has an active visit.")
            : Result<EnrichedRequest, AppError>.Success(request);
    }

    public static Result<EnrichedRequest, AppError> EvaluateAccess(
        EnrichedRequest request) =>
        CheckGuestBlocked(request)
            .Bind(CheckPassValidity)
            .Bind(CheckZoneAccess)
            .Bind(CheckNotInside)
            .Bind(CheckGuestLimit);

    public static Result<AccessDecision, AppError> ProcessEntry(
        SecurityData state,
        AccessRequest request,
        DateTimeOffset now) =>
        EnrichRequest(state, request, now)
            .Bind(EvaluateAccess)
            .Bind(valid => Visit.Create(
                    valid.Pass.Id,
                    valid.Room.Id,
                    now,
                    Option<DateTimeOffset>.None)
                .Bind(visit => state.WithVisits(
                    state.Visits.Append(visit)))
                .Map(nextState => FormatResult(valid, nextState)));

    private static AccessDecision FormatResult(
        EnrichedRequest request,
        SecurityData nextState) =>
        new(
            nextState,
            $"GRANTED: {request.Guest.Name}, pass {request.Pass.Id}, room {request.Room.Id}, " +
            $"building {request.Room.BuildingId}. Host {request.Employee.Id}: " +
            $"{CountActiveGuests(nextState, request.Employee.Id)}/{request.Employee.MaxGuests} guests inside.");

    public static Result<AccessDecision, AppError> ProcessExit(
        SecurityData state,
        string passId,
        DateTimeOffset now) =>
        Find(state.Passes, p => p.Id == passId?.Trim())
            .ToResult(new AppError(
                "PassNotFound",
                $"Pass '{passId}' was not found."))
            .Bind(pass => Find(
                    state.Visits,
                    v => v.PassId == pass.Id && v.IsActive)
                .ToResult(new AppError(
                    "NotInside",
                    $"Pass '{pass.Id}' has no active visit."))
                .Bind(active => active.Exit(now)
                    .Bind(completed => state.WithVisits(
                        state.Visits.Select(
                            v => ReferenceEquals(v, active) ? completed : v)))
                    .Map(nextState => new AccessDecision(
                        nextState,
                        $"EXIT: guest {pass.GuestId}, pass {pass.Id}. " +
                        $"Host {pass.EmployeeId} now has {CountActiveGuests(nextState, pass.EmployeeId)} guests inside."))));

    private static Result<T, AppError> Fail<T>(
        string code,
        string message) where T : notnull =>
        Result<T, AppError>.Failure(new(code, message));
}