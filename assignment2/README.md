# FunctionalSupportDesk

IT-2511 Mihayevich Artur

## Running Code

```bash
dotnet run /directorytproject/
```

## Tests

| Test case                              | Expected                 | Pass |
| -------------------------------------- | ------------------------ | ---- |
| 101\|HIGH\|CANNOT SIGN IN\|2           | Successful parsing       | pass |
| BAD\|CRITICAL\|SERVICE UNAVAILABLE\|4  | Invalid ID failure       | pass |
| 104\|UNKNOWN\|PRINTER PROBLEM\|2       | Invalid priority failure | pass |
| 105\|MEDIUM\|3                         | Empty title failure      | pass |
| 106\|Critical\|Database unavailable\|5 | Successful parsing       | pass |
| 102\|Low\|Change profile photo\|1      | Successful parsing       | pass |
| HIGH TICKET                            | IsUrgent returns true    | pass |
| TICKETS WITH 2, 1, AND 5 HOURS         | Total equals 8           | pass |

## Debugging

```C#
static bool IsUrgent(SupportTicket ticket)
{
    return ((ticket.Priority == TicketPriority.Critical) || (ticket.Priority == TicketPriority.High));
    //logical or
}
static int CalculateTotalHours(IEnumerable<SupportTicket> tickets)
{
    int total = 0;
    foreach (var ticket in tickets)
        total += ticket.EstimatedHours;//summing instead of assigning
    return total;
}
```

## Questions

Which functions are pure, where do side effects remain, and which C# features support the functional style?

Pure unctions are  `ParseTicket`, `GetAction`, `CalculateScore`, `PriorityFirstStrategy`, `QuickFixStrategy`.

Side effects `Console.WriteLine` 

C# features `readonly struct`, `Func<>`

Why does ParseTicket return a failure value instead of throwing for invalid input?

Program expect user to inputinavid data somtimes.

When does the code inside ParseTickets actually execute?

During Lazy Evaluation (asignment requirements), going through every element

How does the Strategy pattern allow the scoring behavior to change?

`CalculateScore` can acept any type of stategies  what can be used to change formula outside of high order function

What does readonly prevent in SupportTicket?

It provides immutablity 

Why does Main call Join after starting the summary thread?

To prevent program from stoping before output
