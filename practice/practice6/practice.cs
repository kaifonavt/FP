#!/usr/bin/env dotnet

Func <T,R> Compose<T,U,R>(Func<T, U> first, Func<U, R> second)
{
    return x => second(first(x));
}
var pipeline = Compose<int, int, int>(x => x + 1, x => x * 3);

Console.WriteLine(pipeline(2));