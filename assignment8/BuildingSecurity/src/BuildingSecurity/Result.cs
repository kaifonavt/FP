namespace BuildingSecurity;

public sealed record AppError(string Code, string Message);

public abstract class Result<T, E>
    where T : notnull
    where E : notnull
{
    private Result() { }

    public sealed class SuccessCase : Result<T, E>
    {
        public T Value { get; }

        internal SuccessCase(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
    }

    public sealed class FailureCase : Result<T, E>
    {
        public E Error { get; }

        internal FailureCase(E error)
        {
            ArgumentNullException.ThrowIfNull(error);
            Error = error;
        }
    }

    public static Result<T, E> Success(T value) =>
        new SuccessCase(value);

    public static Result<T, E> Failure(E error) =>
        new FailureCase(error);

    public Result<U, E> Map<U>(Func<T, U> transform)
        where U : notnull =>
        Match(
            value => Result<U, E>.Success(transform(value)),
            Result<U, E>.Failure);

    public Result<U, E> Bind<U>(Func<T, Result<U, E>> next)
        where U : notnull =>
        Match(
            value => next(value)
                     ?? throw new InvalidOperationException(
                         "Bind callback must return a Result."),
            Result<U, E>.Failure);

    public R Match<R>(
        Func<T, R> success,
        Func<E, R> failure) => this switch
    {
        SuccessCase found => success(found.Value),
        FailureCase rejected => failure(rejected.Error),
        _ => throw new System.Diagnostics.UnreachableException()
    };
}