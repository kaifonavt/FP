Func<int, int, int> add = (a, b) => a + b;

// Каррированная функция: int -> (int -> int)
Func<int, Func<int, int>> curriedAdd = a => b => a + b;
Func<int, Func<int, int>> curriedSubtract = a => b => a - b;

Console.WriteLine(curriedSubtract(10)(4));