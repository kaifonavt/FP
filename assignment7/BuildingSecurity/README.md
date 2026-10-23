# Building Security System: Guest Access Module

## Assignment 7 - Exception Handling in Functional Programming

This C# console module helps a security officer check a guest's temporary pass
before registering entry to a room. It uses sample data and does not connect to
physical access readers, door controllers, or an external database.
Expected refusals are `Result` failures, lookups use `Option`, and JSON/I/O errors
are handled at the file boundary. Domain functions receive data and time as inputs.
All session changes are in memory; restarting loads the original JSON snapshot.

## Build, run, and test

Verified with .NET SDK **8.0.425** and the .NET 8 runtime on Windows.
`global.json` selects SDK 8.0.425 or a later patch in that feature band.
NuGet access is needed for the first restore. No extra application libraries are used.

From the repository root, change into the module directory:

```powershell
cd assignment7/BuildingSecurity
```

Keep this working directory for the following commands. Run each command
separately and wait for it to finish before entering the next one.

```powershell
dotnet restore BuildingSecurity.sln
```

```powershell
dotnet build BuildingSecurity.sln --no-restore --warnaserror
```

```powershell
dotnet test BuildingSecurity.sln --no-build
```

Run the automatic demonstration:

```powershell
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj --no-build -- --demo
```

For interactive use:

```powershell
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj --no-build
```

Other launch options (choose one):

```powershell
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj -- --data "sample-data/security.json"
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj -- --help
```

The default data path is relative to `AppContext.BaseDirectory`, so a compiled
application can run from another working directory. Explicit `--data` and `reload`
paths are absolute or relative to the process working directory. Paths with spaces
are supported; quote a path passed as a command-line argument.

The canonical fixtures are `sample-data/security.json` and
`sample-data/malformed.json`. The project copies both into build output.
Do not restore the old empty root JSON files or keep a second editable fixture copy.

## Console commands

| Command | Behaviour |
| --- | --- |
| `help` | Print available commands. |
| `list` | List guests, employees, rooms, passes and visit counts. |
| `enter P1 101` | Validate and register entry; replace state only on success. |
| `exit P1` | End an active visit; preserve history and release capacity. |
| `lookup 1` | Show `Some(guest 1)` and its properties. |
| `lookup 999` | Show `None: guest not found`. |
| `count 10` | Show `Some(0)` if the known employee has no active guests. |
| `count 999` | Show `None: employee not found`. |
| `reload "sample-data/security.json"` | Validate the entire file before replacing state; refuse if visit history already exists. |
| `quit` | End the session. EOF also ends it. |

Invalid commands report `InvalidCommand` and leave the session usable.
Process exit codes: `0` for a completed session/demo or help, `1` for failed initial
data loading, `2` for invalid launch arguments. A handled command refusal does not
terminate an interactive session. Demo deliberately includes expected failures.

## Reproducible demonstrations

`--demo` uses **2026-10-20T10:00:00Z**, independent of the machine clock, and prints
that choice. Interactive mode uses real UTC time. Sample passes P1/P2/P4/P5 expire
on 2030-01-01; adjust input data for interactive demonstrations after that date.

The sample already contains one active visit for P5, hosted by employee 20.
Run these commands in a fresh session, in this order:

| Input | Expected outcome |
| --- | --- |
| `lookup 1` | Present guest with `blocked=False`; false is legitimate data. |
| `lookup 999` | Absent optional lookup. |
| `count 10` | Present count `Some(0)`; zero is not absence. |
| `enter P1 101` | `GRANTED`, one new active visit. |
| `enter P2 101` | `GuestBlocked`. |
| `enter P3 101` | `PassExpired`. |
| `enter P1 102` | `ZoneAccessDenied`. |
| `enter P4 101` | `HostGuestLimitExceeded`: employee 20 has limit 1 and already hosts P5. |
| `enter P999 101` | `PassNotFound`: missing required lookup becomes a Result failure. |
| `enter P1 101` | `AlreadyInside`, no second visit. |
| `reload sample-data/malformed.json` | `MalformedFile`, state unchanged. |
| `reload sample-data/does-not-exist.json` | `FileNotFound`, state unchanged (keep this path nonexistent). |
| `exit P1` | `EXIT`, the visit remains in history but is inactive. |
| `list` | `Visits: 2 total, 1 active`. |

The automatic demo uses a generated nonexistent filename for the missing-file case.
Its error messages include the operation and file path. In the supplied sample,
reload of even a valid file is refused because history already exists. Start with a
custom valid file containing `"visits": []` to demonstrate successful reload before
the first visit. Reload is intentionally not a history-reset feature.

## Input models, business rules, and state

The input file contains five explicit arrays: `guests`, `employees`, `rooms`,
`passes`, and `visits`. They may be empty but must be present and non-null.

| Model | Main data | Role |
| --- | --- | --- |
| `Guest` | ID, name, blocked flag | The person requesting entry. |
| `Employee` | ID, name, maximum guest count | The host responsible for visitors. |
| `Room` | ID, name, building ID | The requested access zone. |
| `TemporaryPass` | Pass ID, guest/employee/building IDs, validity interval, allowed room IDs | Permission to request entry. |
| `Visit` | Pass ID, room ID, entry time, optional exit time | Active presence or completed visit history. |
| `SecurityData` | Read-only snapshots of all five collections | The current validated session state. |

Entry checks a blocked guest, the pass interval `[ValidFrom, ExpiresAt)`, the allowed
room/building, duplicate active entry, and the host's active guest limit. Four main
domain errors are `GuestBlocked`, `PassExpired`, `ZoneAccessDenied` and
`HostGuestLimitExceeded`. Additional errors include `PassNotStarted`, `AlreadyInside`,
`NotInside`, invalid requests and missing required records.

Factories reject invalid values. A valid model can still be refused by the current
workflow state: for example, an employee with `MaxGuests = 0` is valid data, but entry
fails its capacity check. Lookup absence is another distinct case represented by
`Option.None`. No fabricated guest or default numeric value substitutes for absence.

`SecurityData` copies its collections and validates identities, references and visit
consistency. `ProcessEntry`/`ProcessExit` return a new snapshot. `Program.RunSession`
assigns that snapshot only inside the final Result success handler. On failure it
prints the error and keeps the previous snapshot.

## Functional pipeline and exception boundaries

Entry processing composes the following stages. Each fallible step returns
`Result<T, AppError>` and stops the chain on failure.

| Stage | Input | Successful output | Example refusal |
| --- | --- | --- | --- |
| `EnrichRequest` | State, pass/room request, current time | `EnrichedRequest` with resolved records | `PassNotFound` |
| `EvaluateAccess` | Enriched request | The same request after rule checks | `GuestBlocked`, `PassExpired`, `ZoneAccessDenied`, `HostGuestLimitExceeded` |
| `Visit.Create` and `WithVisits` | Validated request and existing snapshot | New state containing the visit | Invalid visit data or inconsistent state |
| `FormatResult` | Validated request and new state | `AccessDecision` containing state and message | Pure transformation; no new domain check |

`Program.Execute` maps the successful decision to a console outcome.
`RunSession` handles the resulting Success or Failure once, at the application
boundary. Entry and exit rules do not read console input or access files.

`Parser.Load` is the operational boundary: it reads JSON and catches relevant
I/O and deserialization exceptions. It returns an error containing the operation,
path, and reason. A broken file is never replaced with an empty successful state.
`Parser.Reload` validates a new snapshot and refuses to erase existing visit history.

## Files and assignment mapping

| Requirement | Files and methods |
| --- | --- |
| Distinct success/failure cases | `Result.cs`: `SuccessCase`, `FailureCase`, `Match`; payloads are validated. |
| Meaningful domain refusals | `FunctionalPipeline.cs`: `EvaluateAccess`, `CheckGuestBlocked`, `CheckPassValidity`, `CheckZoneAccess`, `CheckGuestLimit`. |
| Real optional/required lookups | `Option.cs`: `Some`, `None`, `ToResult`; `AccessPipeline.Find`, `EnrichRequest`; console `lookup`/`count`. |
| Pure transformation | `Result.Map`; workflow `FormatResult`; console maps an `AccessDecision` into `ConsoleOutcome`. |
| Fallible composition | `Result.Bind`; `EnrichRequest -> EvaluateAccess -> Visit.Create -> WithVisits`. |
| Failure stops dependent callbacks | `Map`/`Bind` preserve the error without invoking the callback. |
| Operational boundary | `Parser.Load`: catches relevant file/JSON exceptions and returns errors with context. |
| Atomic loading and safe reload | `Parser.Build` builds a complete validated snapshot; `Parser.Reload` protects existing history. |
| Final application handling | `Program.Run` handles initial load; `RunSession` handles each command Result once. |
| Normal/refusal/missing/file demos | `Program.RunDemo`, shipped JSON, interactive commands above. |

`Map` takes `T -> U` and wraps the transformed value in a successful Result.
`Bind` takes `T -> Result<U, E>` and preserves the returned success or failure.
Neither continues after a prior failure. Expected business refusals use these cases;
file-system and parsing exceptions are converted to Result at `Parser.Load`.

## Automated tests and latest outcome

Latest verification on **23 October 2026**: **65 passed, 0 failed, 0 skipped**.
Build with warnings as errors: **0 warnings, 0 errors**. The automatic demo and
interactive entry/repeated-entry/exit scenario also completed successfully.
A clean copy of the module, without existing `bin` or `obj` directories, was built,
tested, and run successfully on the same date.

| Test file | Cases | Coverage |
| --- | ---: | --- |
| `FunctionalTest.cs` | 9 | Map/Bind success, unchanged failure, callback counters, Match, Some(false/0), None, required payloads. |
| `ModelTest.cs` | 5 | Validated factories, immutable copies, duplicate identities, references and history. |
| `WorkflowTest.cs` | 16 | Complete success, all four main failures, interval boundaries, missing/present lookup, duplicate entry, exit/capacity and skipped operations. |
| `FileTests.cs` | 17 | Valid JSON, missing/malformed/null data, missing versus explicit false/zero, references, partial-update prevention and reload. |
| `ConsoleTests.cs` | 18 | End-to-end demo and command handling, failure recovery, arguments, EOF, quoted paths, deterministic time and bundled fixtures. |

Key assertions to show at defense:

- `Map_PropagatesSameErrorWithoutCallingCallback` and
  `Bind_PropagatesSameErrorWithoutCallingCallback`: callback count stays zero.
- `CompleteSuccess_ProducesVisitAndMessageWithoutMutatingInput`: correct new state.
- `InvalidReload_DoesNotReplaceExistingStateOrRunSuccessCallback`: same original
  state reference and no successful update callback.
- `FailedReload_PreservesUsableStateInActualConsole`: a subsequent entry still works.
- `ShippedDemo_ShowsAllRequiredOutcomesAndPreservesHistoryAfterFileFailures`: actual
  console output contains every required scenario and correct final visit totals.
- `Reload_HandlesQuotedPathWithSpacesAndAppliesNewState`: real path containing spaces.
- `Demo_UsesDocumentedTimeEvenWhenRealClockIsPastSampleExpiry`: fixed demonstration clock.
- `BundledSamples_AreAvailableFromOutputDirectory`: fixtures are really copied and parse as intended.

## Team contribution record

Names below come from the team commits/local handoff. Missing details must be
completed by the team before submission; a GitHub handle is not a replacement for
the required full name and group. Each member must review and explain the actual code.

| Member | Name / account | Group | Contribution at this stage |
| --- | --- | --- | --- |
| 1 | Artur Mikhayevich / `kaifonavt` | To be supplied | Result/Option, validated models, SecurityData, functional/model tests. |
| 2 | Meiirzhan Baigurman / `mmeeir` | To be supplied | Access pipeline, pure rule checks, entry/exit and workflow tests. |
| 3 | Full name to be supplied / `P4fos` | To be supplied | JSON parser, operational errors, reload rules, sample contents and file tests. |
| 4 | Mukhametzhanov Batyrkhan / `bl1rk` | IT-2511 | Console integration, final Result handling, demonstration, console tests, fixture packaging and documentation. |

See `TEAM_COMMITS.md` for the handoff and file ownership. It does not execute or
prescribe Git operations.

### Member 4: console integration and verification

Mukhametzhanov Batyrkhan, IT-2511, is responsible for the application boundary:
argument and command parsing, final Result handling, readable output, the
demonstration script, console tests, sample-file packaging, and this documentation.
The domain models, access rules, and JSON parser remain the earlier members' parts.

The console test suite checks the integrated workflow rather than duplicating the
domain rules. For example, a failed reload must leave the session usable for the
next entry command, and an invalid command must report a reason without crashing.

## Remaining limitations and next stage

- Session state is in memory; visits are not saved back to JSON.
- The console is a coursework demonstration, not a physical access-control system.
- Interactive mode uses current UTC time; sample pass dates must be maintained.
  The automatic demo uses its documented fixed time for reproducibility.
- The supplied snapshot already contains visit history. Reloading it cannot be
  used to clear history; begin a new session or use an initially empty visit list.
- Complete the missing team names and groups in the contribution table before
  submission. Those details have not been guessed.

This is the reusable BuildingSecurity module for the final-project workflow.
Assignments 5/6 patterns (pipeline and validated factories) are carried forward in
the implementation; their old executables are not invoked from this console.
Assignment 8/final-project integration remains a later project step.

## Submission

For the Assignment 7 ZIP include this module's solution, `global.json`, `src`,
`tests`, `sample-data`, README and team contribution record. Exclude all nested
`bin`, `obj`, `.idea`, `.git`, local caches and secrets. The separate assignment PDF
does not need to be packaged as application source. Re-run the build, tests and demo
from a clean extraction. Every student submits the runnable snapshot in Moodle;
a repository URL alone does not replace it. Complete the missing team details and
be ready to explain your own code and modify a rule/test during defense.
