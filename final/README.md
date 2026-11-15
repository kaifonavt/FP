# Final: Building Security System

A console project using C# 12 / .NET 8. It follows the structure of assignments 7-8: one application project and one test project. The archive contains source code, sample data, and tests. It does not include a compiled application.

## Run

Install .NET SDK 8 or later. Open a terminal in the folder containing `BuildingSecuritySystem.sln`:

```sh
dotnet run --project src/BuildingSecurity
```

Enter `help` to see the commands. Enter `exit` to close the program. You do not need to configure a personal Windows path.

The program reads and saves data in the project's `data` folder. It finds that folder inside `BuildingSecuritySystem`, even when you start the program from a different working directory. To experiment with a copy of the data, copy `data` to `my-data` and use `--data`:

```sh
dotnet run --project src/BuildingSecurity -- --data ./my-data
```

A relative `--data` path starts from your terminal's current working directory. An absolute path also works.

## Project layout

```text
BuildingSecuritySystem/
  BuildingSecuritySystem.sln
  README.md
  data/                         Seven JSON data files
  report/                       Exported JSON reports
  src/BuildingSecurity/         Console application
  tests/BuildingSecurity.Tests/ Tests
```

The archive includes an empty `report` folder. If you remove it, the program creates it when you export a report.

## Reading the code

All application files are in `src/BuildingSecurity`. The application uses one project, with separate files for pure functions and file operations.

| Order | File | What to look at |
|---|---|---|
| 1 | `Models.cs` | Employee, card, room, reservation, ticket, audit entry, and application state |
| 2 | `Functional.cs` | `Result` for success or failure, `Maybe` for a value or its absence, and extension methods |
| 3 | `Access.cs` | Four steps for checking a card and adding an audit entry |
| 4 | `Tickets.cs` | Creating and resolving tickets, including a repeated resolution check |
| 5 | `Reservations.cs` | Room access, overlapping times, and cancellation |
| 6 | `Reports.cs` | LINQ grouping, counting, and sorting for two reports |
| 7 | `Validation.cs` | Data checks after reading JSON |
| 8 | `CommandLine.cs`, `CommandProcessor.cs` | Parsing a command and choosing a function |
| 9 | `Program.cs`, `CliSession.cs` | Startup, command loop, output, and saving |
| 10 | `JsonStore.cs` | Reading seven files, saving data, and exporting reports |

`ProjectPaths.cs` locates the source project and its data and report folders. Read it last; it contains no business rules. `GlobalUsings.cs` imports immutable collections.

One command follows this path:

```text
Program -> CliSession -> CommandLine -> CommandProcessor
                                      -> domain function -> new state
           CliSession -> JsonStore -> result output
```

In `CliSession`, the steps are: execute the command, check the result, save the file, and update the state. If saving fails, the session keeps the old state.

## Functional programming techniques

- A `record` describes data. A `with` expression creates a copy with changes.
- `ImmutableArray` lets us add entries to a new collection while keeping the previous collection.
- `Result<T>.Ok` contains a result; `Result<T>.Fail` contains an error.
- `Map` transforms a successful value. `Bind` runs the next step, which can also fail. After `Fail`, the chain skips dependent steps.
- `Maybe<T>.Some` contains a matching entry; `None` represents its absence.
- We can obtain a `VerifiedCard` only after checking the card and employee. Resolving an open ticket produces a `ResolvedTicket`; resolving it again returns an error.
- `AccessPolicy` is a rule function. The building mode selects the rule for the next access attempt.
- `CheckAccess(policy)(time)(room)(verified)` supplies one argument at a time. This demonstrates currying and partial application.
- `StreamWhere` uses `yield return`, so filtering starts during enumeration. The command calls `ToList()` to create a snapshot for output.

The assignment requires these techniques. The application shell handles the clock, GUIDs, console, and files; business functions receive values as parameters.

## Example session

In the sample data, `c1` is an active card for an employee with clearance level 5, `c2` has level 2, and `c4` belongs to an inactive employee. Room `r1` is a meeting room; `r2` is a server room that requires level 5.

```text
config set-mode night
scan-card c1 r2
scan-card c2 r2
support-ticket create e1 "The lock does not work"
support-ticket resolve t1
support-ticket resolve t1
reservation book r1 e2 2026-11-14T10:00:00+05:00 2026-11-14T11:00:00+05:00
reservation book r1 e2 2026-11-14T10:30:00+05:00 2026-11-14T11:30:00+05:00
audit-log stream --denied-only
audit-log anomalies --threshold 3
report access-summary --export access.json
report room-utilization --export "room utilization.json"
exit
```

The first scan grants access; the second denies it. Resolving `t1` a second time returns an error. The second reservation overlaps the first, so the program rejects it. You can continue entering commands after an error.

## Rules and commands

The project includes all six modules from the assignment. Enter `help` for the full command syntax.

- `scan-card`: checks card and employee activity, clearance, and building mode, then records the result in the audit log.
- `support-ticket`: `list [--status open|closed]`, `create`, and `resolve`.
- `reservation`: `book`, `cancel`, and `list`. Adjacent time intervals are valid. A cancelled reservation does not block a new one.
- `audit-log`: `stream` with filters and `anomalies` with a positive threshold. The default threshold is 3 denials across the whole log.
- `report`: `access-summary` and `room-utilization`, with an optional `--export` filename.
- `config`: `show`, `set-mode normal|night|lockdown`, and `set-hours HH:mm HH:mm`.

In `normal` mode, working hours apply. In `night` mode, employees need clearance level 3. In `lockdown` mode, they need the `lockdownMinLevel` from `config.json`, which defaults to 5. The program checks the room's required clearance in each mode. Working hours can cross midnight; the start time is inclusive and the end time is exclusive.

Reservation dates include a time offset such as `+05:00` or `Z`. IDs are case-sensitive. Put descriptions and filenames with spaces in quotes. When you create a ticket or reservation, the program prints its ID for later resolution or cancellation.

## Export reports

Use `--export` with a JSON filename. The program saves reports in `BuildingSecuritySystem/report`, regardless of the terminal's working directory or a `--data` override:

```text
report access-summary --export access.json
report access-summary --export "access report.json"
report room-utilization --export room-utilization.json
```

These commands save `report/access.json`, `report/access report.json`, and `report/room-utilization.json`. Use a filename ending in `.json`, starting with a letter or digit, with up to 128 characters. Letters, digits, spaces, periods, hyphens, and underscores are allowed. Paths and subfolders are not allowed. For example, `../access.json`, `reports/access.json`, and absolute paths return an error. The seven data filenames (such as `rooms.json`) are reserved. Exporting again to the same report filename replaces that report. Generated report JSON files are ignored by Git.

## Build and tests

Run these commands from the folder containing the solution:

```sh
dotnet build BuildingSecuritySystem.sln -c Release
dotnet test BuildingSecuritySystem.sln -c Release
```

The tests cover pure functions, complete commands, file operations, and boundary cases. They are in `tests/BuildingSecurity.Tests` and use xUnit and FluentAssertions.

JSON files must contain all required fields. The program reports missing files, invalid JSON, and domain errors. It writes through a temporary file before replacing the destination. Use one program instance for each copy of the data.

Before presenting the project, study the functions and tests you plan to explain. This archive contains the final project's code. Personal and group study reports are separate deliverables.
