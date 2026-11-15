namespace BuildingSecurity;

public enum ErrorCode
{
    EntityNotFound, InactiveAccountOrCard, InsufficientClearance, AccessDenied,
    TimeSlotConflict, TicketAlreadyResolved, AlreadyCancelled, InvalidInput, StorageError
}
public sealed record SecurityError(ErrorCode Code, string Message);

public abstract record Result<T>
{
    private Result() { }
    public sealed record Ok(T Value) : Result<T>;
    public sealed record Fail(SecurityError Error) : Result<T>;
    public static Result<T> Success(T value) => new Ok(value);
    public static Result<T> Failure(ErrorCode code, string message) => new Fail(new(code, message));
    public U Match<U>(Func<T, U> ok, Func<SecurityError, U> fail) => this is Ok success ? ok(success.Value) : fail(((Fail)this).Error);
}

public abstract record Maybe<T>
{
    private Maybe() { }
    public sealed record Some(T Value) : Maybe<T>;
    public sealed record None : Maybe<T>;
}

public static class FunctionalExtensions
{
    public static Result<U> Map<T, U>(this Result<T> source, Func<T, U> map) =>
        source.Match(x => Result<U>.Success(map(x)), e => new Result<U>.Fail(e));
    public static Result<U> Bind<T, U>(this Result<T> source, Func<T, Result<U>> bind) =>
        source.Match(bind, e => new Result<U>.Fail(e));
    public static Result<T> ToResult<T>(this Maybe<T> source, SecurityError error) => source switch
    {
        Maybe<T>.Some(var value) => Result<T>.Success(value),
        _ => new Result<T>.Fail(error)
    };
    public static Maybe<T> FindMaybe<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source) if (predicate(item)) return new Maybe<T>.Some(item);
        return new Maybe<T>.None();
    }
    public static IEnumerable<T> StreamWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source) if (predicate(item)) yield return item;
    }
    public static Result<T> FindEntity<T>(this IEnumerable<T> source, Func<T, bool> predicate, string name) =>
        source.FindMaybe(predicate).ToResult(new(ErrorCode.EntityNotFound, $"Not found: {name}"));
}
