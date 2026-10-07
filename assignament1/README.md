# TIcket Price Calculator

Mikhayevich Artur IT-2511

## How to run

```bash
dotnet run /directorytoProgram/
```

## Tests

| **Price** | Age | Student | Ticket   | Day     | Result  |
| --------- | --- | ------- | -------- | ------- | ------- |
| 5000      | 5   | false   | Standard | Weekday | 0.00    |
| 5000      | 10  | false   | Standard | Weekday | 2500.00 |
| 5000      | 20  | true    | Standard | Weekday | 4250.00 |
| 5000      | 65  | false   | Standard | Weekday | 3500.00 |
| 5000      | 30  | false   | Vip      | Weekday | 6250.00 |
| 5000      | 30  | false   | Standard | Weekend | 5500.00 |
| 5000      | 20  | true    | Vip      | Weekend | 5843.75 |

Also negative price, empty input, text instead of age, unknown enum values, null, and zero price gives early return.

## Questions

**Which parts of the program are imperative?**

`Main` function because it controls Input and output interact with them directly.

`Console` related methods in Main are imperative 

**Which functions are pure?**

```C#
decimal priceByAge = ApplyPricingRule(price, AgeRule);
decimal priceByTicketType = ApplyPricingRule(priceByAge, TicketTypeRule);
decimal priceByDayType = ApplyPricingRule(priceByTicketType, DayTypeRule);
decimal priceFinal = Math.Max(0.00m, Math.Round(priceByDayType, 2, MidpointRounding.AwayFromZero));
```

These functions are pure because it has no side effects and determined 

**Where do side effects remain?**

It remains in console and Input variables in Main, which is Imperative shell.

**Why is TryParse preferred to Parse for user input?**

It throws an exception, catches error data (try/catch), helps to early exit for unsuitable data.

 
