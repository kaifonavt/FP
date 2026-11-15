using System.Diagnostics;

namespace BuildingSecurity;

public sealed record AppError(string Code, string Message);

public abstract class Result<T, E>
    where T : notnull
    where E : notnull
{
    private Result()
    {
    }

    public static Result<T, E> Success(T value)
    {
        return new SuccessCase(value);
    }

    public static Result<T, E> Failure(E error)
    {
        return new FailureCase(error);
    }

    public Result<U, E> Map<U>(Func<T, U> transform)
        where U : notnull
    {
        return Match(
            value => Result<U, E>.Success(transform(value)),
            Result<U, E>.Failure);
    }

    public Result<U, E> Bind<U>(Func<T, Result<U, E>> next)
        where U : notnull
    {
        return Match(
            value => next(value)
                     ?? throw new InvalidOperationException(
                         "Bind callback must return a Result."),
            Result<U, E>.Failure);
    }

    public R Match<R>(
        Func<T, R> success,
        Func<E, R> failure)
    {
        return this switch
        {
            SuccessCase found => success(found.Value),
            FailureCase rejected => failure(rejected.Error),
            _ => throw new UnreachableException()
        };
    }

    public sealed class SuccessCase : Result<T, E>
    {
        internal SuccessCase(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }

        public T Value { get; }
    }

    public sealed class FailureCase : Result<T, E>
    {
        internal FailureCase(E error)
        {
            ArgumentNullException.ThrowIfNull(error);
            Error = error;
        }

        public E Error { get; }
    }
}