# Building Security - Assignment 7

## Current status

This is a compiling project scaffold, not a completed assignment. Access rules,
Result/Option operations, file loading and automated tests are not implemented.
The two JSON files are preserved from the original project and currently contain
empty objects. `malformed.json` is not malformed yet; prepare its demonstration
content when implementing the parser.

## Layout and continuity

The layout follows Assignment 5 (CardAccess): `src`, `tests`, a local solution,
and JSON files at the module root. It uses .NET 8 and the xUnit package versions
from Assignment 6 (SupportTicket).

- `src/BuildingSecurity/Program.cs`: console boundary and final outcome handling.
- `Guest.cs`, `Employee.cs`, `Room.cs`, `TemporaryPass.cs`, `Visit.cs`: separate
  model scaffolds, following the separate User/Card/Room files in Assignment 5.
  Room represents the requested access zone. Building and host identifiers must
  be defined consistently when implementing these models.
- `FunctionalPipeline.cs` / `AccessPipeline`: lookup, validation and result
  formatting, following Assignment 5's pipeline organization.
- `Parser.cs`: file/JSON boundary, following Parser and UserParser in earlier work.
- `Result.cs`: explicit success/failure and Map/Bind, still to implement.
- `Option.cs`: explicit present/absent lookup outcomes, still to implement.
- `tests/BuildingSecurity.Tests/WorkflowTest.cs`: empty test class, matching the
  existing projects' compact test organization. It does not count as a test.

Carry forward the relevant Assignment 5 pipeline logic and Assignment 6 validated
factory/case-model logic during implementation. No references to, or changes in,
those projects were added by this structural change. Their runtime integration
with Assignment 7 remains implementation work.

## Agreed workflow and four domain failures

Check entry using an existing temporary guest pass:

1. Guest is blocked: `GuestBlocked`.
2. Current time is at or after the pass expiry: `PassExpired`.
3. Requested zone/building is not allowed by the pass: `ZoneAccessDenied`.
4. The host already has the maximum number of active guests inside:
   `HostGuestLimitExceeded`.

No host-approval check is required in this version. Count active visits, not all
historical visitors. An exit ends an active visit and releases one place. Reject
duplicate active entry and prevent entry before the pass start time as consistency
rules. Do not register a visit before every required check succeeds.

## Implementation boundaries

- Use Option for real lookups and convert a missing required record to a useful
  Result failure. A valid false/zero value is not absence.
- Compose the workflow with Bind and Map. Neither may invoke its callback after
  failure. Handle the final result once in Program.
- Keep domain checks pure. Pass current time and data as inputs.
- Model success/failure cases without contradictory states; use validated
  construction where appropriate, as in Assignment 6.
- Catch relevant I/O/parsing exceptions in Parser. Parse and validate a complete
  new snapshot before replacing any application state. Do not silently return an
  empty successful result for malformed or missing data.

## Build, run and test

Prerequisites: an SDK capable of targeting net8.0, the .NET 8 runtime and NuGet
access on first restore. The restructuring environment has SDK 9.0.305.

Run from `assignment7/BuildingSecurity`:

```sh
dotnet restore BuildingSecurity.sln
dotnet build BuildingSecurity.sln --no-restore
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj --no-build
dotnet test BuildingSecurity.sln --no-build
```

The current console only identifies the scaffold; no access decision is made.
The test project is connected, but no executable tests have been written yet.
JSON files are copied into build output using the same project configuration
pattern as Assignments 5 and 6. The eventual parser should resolve input paths
explicitly (for example relative to AppContext.BaseDirectory), or document the
required working directory consistently.

## Test coverage to implement

Provide at least 10 cumulative assertion-based tests covering the integrated
success, all four domain failures and their errors, present/absent lookup,
legitimate false/zero values, Map success, Bind success, and preserved failures
with callbacks not invoked. Also cover missing/malformed files and no partial
state updates, duplicate entry, and capacity released after exit. File tests must
use controlled temporary paths and clean up their resources.

## Team and submission

The four-person team assigns responsibilities separately. Each participant must
implement meaningful logic, add automated tests and document their name, group
and contribution before submission. This scaffold does not assign contributors.

Before submission, document implemented methods, examples, test names and the
actual latest test outcome. Include source, project/solution files, tests, README
and sample JSON in the ZIP. Exclude bin, obj, IDE caches and secrets. Verify the
instructions from a clean extraction.