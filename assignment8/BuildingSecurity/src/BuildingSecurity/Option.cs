using System.Diagnostics;

namespace BuildingSecurity;

public abstract class Option<T> where T : notnull
{
    private Option()
    {
    }

    public static Option<T> None { get; } =
        new NoneCase();

    public static Option<T> Some(T value)
    {
        return new SomeCase(value);
    }

    public R Match<R>(
        Func<T, R> some,
        Func<R> none)
    {
        return this switch
        {
            SomeCase found => some(found.Value),
            NoneCase => none(),
            _ => throw new UnreachableException()
        };
    }

    public Result<T, E> ToResult<E>(E error)
        where E : notnull
    {
        return Match(
            Result<T, E>.Success,
            () => Result<T, E>.Failure(error));
    }

    public sealed class SomeCase : Option<T>
    {
        internal SomeCase(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }

        public T Value { get; }
    }

    public sealed class NoneCase : Option<T>
    {
        internal NoneCase()
        {
        }
    }
}