#!/usr/bin/env dotnet
//Shared teaching types

public record Product(string Id, int Stock, decimal Price);
public record Quote(string Id, int Quantity, decimal Total);
public abstract record Maybe<T>
{
    private protected Maybe() { }
    public sealed record Some(T Value) : Maybe<T>;
    public sealed record None : Maybe<T>;
}
public abstract record Result<T>
{
    private protected Result() { }
    public sealed record Ok(T Value) : Result<T>;
    public sealed record Fail(string Error) : Result<T>;
}

public static class R
{
    public static Result<U> Map<T, U>(Result<T> r, Func<T, U> f) => r switch 
    {
        Result<T>.Ok(var x) => new Result<U>.Ok(f(x)),
        Result<T>.Fail(var e) => new Result<U>.Fail(e),
        _ => throw new ArgumentException("Unknown case")
    };

    public static Result<U> Bind<T, U>(Result<T> r, Func<T, Result<U>> f) => r switch
    {
        Result<T>.Ok(var x) => f(x),
        Result<T>.Fail(var e) => new Result<U>.Fail(e),
        _ => throw new ArgumentException("Unknown case")
    };

    //Fix Describe so Some(0) is not reported as missing. Handle None explicitly.
    static string Describe(Maybe<int> value) => value switch
    {
        Maybe<int>.Some(var n) when n != 0 => $"Present:{n}",
        _ => "Missing"
    };

    static Maybe<Product> Find(Product[] items, string id)
    {
        // TODO lookup and return the appropriate case.
        throw new NotImplementedException();
    }
    public static void Main()
    {
    
        
    
    }
}

