# Practice A Tasks

---

## Task 1 Fix delivery aggregation

**6 points total | Code 4 | Written explanation 2**

Return the total Weight of valid Express deliveries. Ignore other modes and invalid lines, and continue processing later
lines. Return 0 for empty input or no matching deliveries.

Each line has the format `Mode;Weight`. The provided `TryParseLine` helper is correct: it accepts Standard, Express,
Priority and an integer from 1 to 6 inclusive, trims spaces around both fields, and rejects malformed or null lines.
Assume the input sequence itself is non-null.

Correct exactly two logical errors in the method below. Keep the signature and foreach loop.

```csharp
public static int SumExpressWeights(IEnumerable<string> lines)
{
    int total = 0;
    foreach (string line in lines)
    {
        if (!TryParseLine(line, out string mode, out int weight))
            return total;

        if (mode == "Express")
            total = weight;
    }
    return total;
}
```

- **Example input:** `"Express;2"`; `"Standard;3"`; `"bad"`; `"Express;4"`.
- **Expected result:** 6.

In `DemoTask1`, add two `Check(label, expectedInt, actualInt)` calls. Each input must expose a different bug: fail
before the fixes and pass afterwards. Do not reuse the whole provided example.

### Required explanation Q1

Why can returning total at the first invalid line make the result depend on input order? Give two orderings of the same
lines that produce different results in the original buggy method.

### Assessment

CRITERION

POINTS

FIRST LOGICAL ERROR CORRECTED

1

SECOND LOGICAL ERROR CORRECTED

1

TWO DIAGNOSTIC CHECKS EXPOSING DIFFERENT ERRORS

1 + 1

WRITTEN EXPLANATION Q1

2

---

## Task 2 Pass a scoring function

**8 points total | Code 6 | Written explanation 2**

### Provided model

```csharp
public enum DeliveryMode
{ Standard, Express, Priority }

public record Delivery(
    int Id, string Destination, int Weight,
    bool IsReady, DeliveryMode Mode);
```

Assume valid non-null records: unique positive IDs, non-null text, Weight from 1 to 6, and only the declared enum
values. The rule parameter is non-null. Do not add validation for these assumptions.

### Methods to implement

```csharp
public static int Evaluate(
    Delivery item, Func<Delivery, int> rule)

public static int BaseScore(Delivery item)
```

1. `Evaluate` must call the supplied rule with `item` and return its result. It must work with any supplied scoring
   function.
2. `BaseScore` must use a switch expression on `Mode`. Select the base points from the table, then add `Weight`.

MODE

BASE POINTS

STANDARD

5

EXPRESS

10

PRIORITY

20

3. In `DemoTask2`, call `Evaluate` with two different lambdas. The first returns `BaseScore`; the second returns
   `BaseScore` plus `Weight` once more. Print both returned values.

Both methods and lambdas must be pure: no console or file operations, no changes to the input, and no changes to
external state.

### Example

For Express with $text{Weight} = 3$, the two results are 13 and 16. The sample object is provided in the starter.

### Required explanation Q2

What does `Evaluate(sample, x => 100)` return, and why? Would `BaseScore` remain pure if it called `Console.WriteLine`?
Explain.

### Assessment

CRITERION

POINTS

EVALUATE INVOKES THE SUPPLIED RULE

1

CORRECT MAPPING USING A SWITCH EXPRESSION

2

ADDS THE NUMERIC FIELD TO THE BASE POINTS

1

PURE METHODS AND LAMBDAS

1

TWO CORRECT LAMBDA CALLS AND PRINTED RESULTS

1

WRITTEN EXPLANATION Q2

2

---

## Task 3 Adjacent sums

**10 points total | Code 8 | Written explanation 2**

Return the sum of each value and the value immediately before it.

```csharp
public static IEnumerable<int> AdjacentSums(
    IEnumerable<int> values)
```

### Requirements

- Use `yield return`.
- Traverse `values` once per enumeration and keep a fixed number of local variables.
- Do **not** use LINQ, indexing, `Count()`, repeated traversal, a temporary array or list, or shared mutable fields.
- The sequence must be lazy: calling the method must not consume input values. Empty input and a single input value must
  both produce an empty result. Preserve the original order of neighbouring pairs.
- Assume `values` is non-null and every integer is from -1000 to 1000. Do not change the input or print inside the
  iterator. Demonstration calls are already provided; you do not need to write them.

### Examples

INPUT

EXPECTED RESULT

3, 5, 2, 8

8, 7, 10

4, 4, 1

8, 5

-3, 1, -2

-2, -1

7

<empty>

<EMPTY>

<empty>

### Required explanation Q3

After `var result = AdjacentSums(new[] { 3, 5, 2 });`, has the input been traversed? If a consumer then requests only
the first result, which input values must be read and what is that result? Explain.

### Assessment

CRITERION

POINTS

CORRECT PAIR RESULTS AND ORIGINAL ORDER

3

LAZY GENERATION WITH YIELD RETURN

2

ONE TRAVERSAL AND FIXED EXTRA STORAGE

1

WRITTEN EXPLANATION Q3

2