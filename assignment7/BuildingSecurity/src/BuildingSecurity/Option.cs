namespace BuildingSecurity;

public abstract class Option<T> where T : notnull
{
    private Option() { }

    public sealed class SomeCase : Option<T>
    {
        public T Value { get; }

        internal SomeCase(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
    }

    public sealed class NoneCase : Option<T>
    {
        internal NoneCase() { }
    }

    public static Option<T> Some(T value) =>
        new SomeCase(value);

    public static Option<T> None { get; } =
        new NoneCase();

    public R Match<R>(
        Func<T, R> some,
        Func<R> none) => this switch
    {
        SomeCase found => some(found.Value),
        NoneCase => none(),
        _ => throw new System.Diagnostics.UnreachableException()
    };

    public Result<T, E> ToResult<E>(E error)
        where E : notnull =>
        Match(
            Result<T, E>.Success,
            () => Result<T, E>.Failure(error));
}