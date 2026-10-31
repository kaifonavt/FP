namespace BuildingSecurity;

public sealed class Visit
{
    public string PassId { get; }
    public int RoomId { get; }
    public DateTimeOffset EnteredAt { get; }
    public Option<DateTimeOffset> ExitedAt { get; }

    public bool IsActive =>
        ExitedAt is Option<DateTimeOffset>.NoneCase;

    private Visit(
        string passId,
        int roomId,
        DateTimeOffset enteredAt,
        Option<DateTimeOffset> exitedAt) =>
        (PassId, RoomId, EnteredAt, ExitedAt) =
        (passId, roomId, enteredAt, exitedAt);

    public static Result<Visit, AppError> Create(
        string? passId,
        int roomId,
        DateTimeOffset enteredAt,
        Option<DateTimeOffset> exitedAt)
    {
        if (string.IsNullOrWhiteSpace(passId)
            || roomId <= 0
            || exitedAt is null
            || exitedAt.Match(exit => exit < enteredAt, () => false))
        {
            return Result<Visit, AppError>.Failure(
                new("InvalidVisit",
                    $"Visit for pass '{passId}': pass/room are required and exit cannot precede entry."));
        }

        return Result<Visit, AppError>.Success(
            new Visit(passId.Trim(), roomId, enteredAt, exitedAt));
    }

    public Result<Visit, AppError> Exit(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return Result<Visit, AppError>.Failure(
                new("AlreadyExited",
                    $"Visit for pass '{PassId}' is already completed."));
        }

        if (now < EnteredAt)
        {
            return Result<Visit, AppError>.Failure(
                new("InvalidExitTime",
                    $"Exit for pass '{PassId}' cannot precede entry."));
        }

        return Create(
            PassId,
            RoomId,
            EnteredAt,
            Option<DateTimeOffset>.Some(now));
    }
}