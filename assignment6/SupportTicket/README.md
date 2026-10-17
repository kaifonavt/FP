# Building Security System: Support Ticket Module

## Assignment 6 - Handling Temporal Coupling

This C# console module creates and scores incident tickets for a building security system. A registered user can report an incident such as a failed entrance alarm, a camera fault, or a damaged access reader. The application checks the user ID, creates a validated ticket, and calculates its priority score.

The current implementation covers ticket validation and scoring. It does not control security equipment, authenticate credentials, dispatch guards, or store tickets. Building security is the application context; the source retains the `SupportTicket` names.

Source reviewed: [kaifonavt/FP](https://github.com/kaifonavt/FP/tree/70f0d2888cd471a5cf8a5fc27b66f7715b10dd25/assignment6/SupportTicket), commit `70f0d2888cd471a5cf8a5fc27b66f7715b10dd25`. See [REPORT.md](REPORT.md) for the design analysis and remaining gaps.

## Build, run, and test

Prerequisites: a .NET SDK capable of building `net8.0`, the .NET 8 runtime, and access to NuGet for the first dependency restore. Verification used SDK **9.0.305** with the installed .NET 8 runtime. This snapshot has no `global.json` SDK pin.

Run these commands from the repository root:

```sh
cd assignment6/SupportTicket
dotnet restore SupportTicket.sln
dotnet build SupportTicket.sln --no-restore
dotnet test SupportTicket.sln --no-build --no-restore
dotnet run --project src/SupportTicket/SupportTicket.csproj --no-build --no-restore
```

Keep `assignment6/SupportTicket` as the working directory when running the application: `Program.Main` opens the relative path `users.json`. The project also copies this file to the build output, but `dotnet run` does not change the working directory to that output folder. No database, API keys, or external service is required.

## Input, rules, and output

Enter an authorized user ID, a ticket ID, an incident description, and a severity number. The supplied users are:

| ID | Name |
| --- | --- |
| 159004 | Alice Smith |
| 257383 | Bob Jones |
| 110901 | Charlie Kirk |

`Program.Main` requires the user ID to appear in `users.json`. The JSON `level` fields are not part of the `User` model and do not grant or restrict permissions.

`Ticket.Create` returns the first applicable error:

| Rule | Failure message |
| --- | --- |
| Ticket ID must be greater than zero | `Ticket ID must be greater than zero.` |
| Description must contain non-whitespace text | `Description cannot be empty.` |
| User must not be null | `User cannot be null.` |
| Level must be a defined enum value | `Invalid ticket level specified.` |

`User.Create` checks a positive ID and nonblank first and last names, trims both names, and throws an argument exception on invalid input. The application loads users through JSON deserialization rather than through this factory; therefore, those factory checks do not validate the loaded registry.

The scoring formula is:

```text
score = base score for severity * multiplier for ticket ID
```

| Severity input | Enum | Base score |
| --- | --- | ---: |
| 0 | Basic | 10 |
| 1 | Medium | 30 |
| 2 | High | 100 |
| 3 | Critical | 300 |

| Ticket ID | Multiplier |
| --- | ---: |
| 1-99 | 0 |
| 100-199 | 1 |
| 200-299 | 2 |
| 300 and above | 3 |

The implementation derives the multiplier from the numeric ticket ID. It does not inspect the description or a separate incident category. Consequently, even a Critical ticket with ID 1 has score 0. These are the current demonstration rules, not an established building safety risk model.

Successful output uses `ticketId|description|firstName lastName|score`. The program handles one attempt per run and exits after reporting a result or a validation error.

## Temporal coupling: before and after

The prerequisite is a complete, valid ticket before scoring. With a mutable ticket, a caller would have to remember to assign and validate every field before evaluation. This illustrative unsafe design exists only in documentation; it is not a claim that these exact methods appeared in an earlier commit:

```csharp
// Unsafe pseudocode: not part of the submitted application.
var ticket = new MutableTicket();
ticket.Id = 250;
int score = ticket.Evaluate(); // Called before setting severity or validating.
ticket.Level = Level.High;
ticket.User = reporter;
ticket.Description = "East entrance alarm failure";
ticket.Validate();             // Too late to protect the earlier calculation.
```

Under the current scoring rules, an unchecked default `Basic` level would yield 20 for ID 250 instead of the intended High score of 200. Omitting validation could also allow an incident without a reporter or description to reach scoring.

The actual API makes construction the prerequisite:

```csharp
var reporter = User.Create(159004, "Alice", "Smith");
TicketResult result = Ticket.Create(
    250, "East entrance alarm failure", reporter, Level.High);

switch (result)
{
    case ValidTicket valid:
        Console.WriteLine(Ticket.Evaluate(valid.Ticket)); // 200
        break;
    case InvalidTicket invalid:
        Console.WriteLine(invalid.Error);
        break;
}
```

This shows the factory-and-match sequence used in `Program.Main`; the console obtains its reporter from the registry instead of calling `User.Create` here.

`Ticket` is sealed, its constructor is private, and its properties have getters only. External callers cannot use `new Ticket(...)`, `new Ticket()`, or assign `ticket.level = Level.High`. These calls fail at compile time and must not be placed in compiled source. The factory checks all four rules before allocating a `Ticket`.

The factory returns one of two declared result cases:

| Case | Payload | Application behavior |
| --- | --- | --- |
| `ValidTicket` | Validated `Ticket` | Evaluate and print its score |
| `InvalidTicket` | Error string | Print the reason and skip evaluation |

These are construction outcomes, not lifecycle states such as Open or Resolved. `Program.Main` matches both cases without casting the result or assuming a payload from the wrong case. `Ticket.cs` also uses switch expressions for severity and ID scoring.

`Ticket.Evaluate` returns an integer without changing the ticket or user. Repeated evaluation of the same ticket returns the same score. Repeated creation with the same ID creates a separate object: there is no ticket store or duplicate-ID check.

The guarantees have limits: `TicketResult` is an extensible public abstract record, result wrappers have public constructors, and `Evaluate` has no explicit null guard. The current factory produces valid payloads, but the entire public API is not a closed, null-safe union. Authorization belongs to `Program.Main`; `Ticket.Create` checks only that the supplied user is non-null.

## Demonstrations

Start a fresh run for each console scenario and enter the listed values at successive prompts.

| Scenario | Input in order | Expected result |
| --- | --- | --- |
| Valid building incident | `159004`, `250`, `East entrance alarm failure`, `2` | `250\|East entrance alarm failure\|Alice Smith\|200` |
| Missing registered-user prerequisite | `999999` | `Error: User ID does not exist in authorized users record.`; no ticket prompts or score |
| Invalid ticket ID | `159004`, `0`, `East entrance alarm failure`, `2` | `Error creating ticket: Ticket ID must be greater than zero.` |
| Empty description | `159004`, `250`, blank line, `2` | `Error creating ticket: Description cannot be empty.` |
| Invalid severity selection | `159004`, `250`, `Camera fault`, `9` | `Invalid Ticket Level choice.`; console rejects input before the factory |

The automated `Create_NullUser_ReturnsInvalidTicket` test covers the absent-user prerequisite at the factory boundary. `Evaluate_RepeatedCall_ReturnsSameScore` evaluates a High ticket with ID 250 twice and checks equality. The code yields 200 on each call, though that test asserts equality rather than the numeric value.

## Files and assignment mapping

| Requirement | Evidence |
| --- | --- |
| Task 1: hidden dependency | Before/after example above; valid construction must precede scoring |
| Task 2: validated factory | `src/SupportTicket/Ticket.cs`: `Create`, private constructor, getter-only properties; used in `Program.Main` |
| Task 3: distinct cases | `TicketResult`, `ValidTicket`, `InvalidTicket` in `Ticket.cs`; hierarchy closure remains a gap |
| Task 4: pattern matching | `src/SupportTicket/Program.cs`: result switch; `Ticket.cs`: `getBaseScore` and `getDangerRate` |
| Task 5: safe operation | `Ticket.Evaluate`, early rejection in `Program.Main`, non-mutating construction and scoring |
| Registry input | `src/SupportTicket/UserParser.cs`: `ParseUsersFromFile`; `users.json` |
| Automated tests | `test/SupportTicket.Tests/UnitTest.cs`: `TicketWorkflowTests` |

## Automated tests and latest outcome

Verified on **17 October 2026** against the source commit identified above. Build: **0 errors, 4 nullable warnings** in `Program.cs`. Tests: **8 passed, 0 failed, 0 skipped**. Successful scoring, unknown-user rejection, and zero-ID rejection also passed separate console smoke checks. The verification build used `-p:NuGetAudit=false`; these results do not represent a dependency security audit.

| Test method | Assertion / expected result |
| --- | --- |
| `Create_ValidInputs_ReturnsValidTicketWithCorrectData` | Valid result; ID 100 and description `Fix database` preserved |
| `Create_NegativeId_ReturnsInvalidTicketWithMessage` | ID -5 rejected with the positive-ID error |
| `Create_BoundaryIdOne_ReturnsValidTicket` | Minimum allowed ID 1 accepted |
| `Evaluate_ValidTicket_CalculatesCorrectScore` | ID 150, Medium: score 30 |
| `PatternMatching_HandlesValidAndInvalidCasesCorrectly` | Both result cases produce the expected string in a test-local switch |
| `Create_NullUser_ReturnsInvalidTicket` | Missing user rejected with the null-user error |
| `Evaluate_RepeatedCall_ReturnsSameScore` | Two evaluations return equal scores |
| `UserIsValid_InvalidId_DoesNotModifyUsersList` | Unknown ID rejected; list count and first user's ID remain unchanged |

Eight passing tests meet the numerical minimum, but they do not establish complete assignment coverage. Missing checks include factory rejection and reasons for blank descriptions and undefined levels; full payload assertions on success; and preservation assertions around failed ticket creation. The existing preservation test checks only list count and one ID. The matching test uses its own switch rather than executing `Program.Main`.

## Remaining limitations and next stage

- Close or constrain the result hierarchy and reject unexpected cases explicitly at the application boundary. The current console switch has no default branch.
- Validate deserialized users, and make registry membership an explicit domain prerequisite if authorization must hold outside the console.
- Address the four nullable warnings and provide deliberate handling for missing or malformed `users.json`.
- Extend the tests listed above, including scoring thresholds 99/100, 199/200, and 299/300.
- Integrate this reusable module with the earlier project workflow for Assignments 7-8. This folder currently builds as its own solution; shared-project integration is not demonstrated here.

For an assignment archive, include this solution, both project directories, `users.json`, and the documentation. Exclude `bin`, `obj`, IDE caches, and secrets. Preserve the directory layout so the run commands and sample input path remain valid.
