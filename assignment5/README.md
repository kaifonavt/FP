# ID Card Access Verification - Assignment 5

A C# console project for a building security system. The application checks a presented card and returns an access decision with a reason. The intended user is a security officer. This demonstration uses sample data and does not connect to physical card readers or door controllers.

## Requirements and commands

Install the .NET 8 SDK. The project targets `net8.0`; `global.json` selects an available SDK from the 8.0 release family. The project was verified with SDK 8.0.425 and has no external NuGet dependencies.

Run these commands from the directory containing `CardAccess.csproj`:

```sh
dotnet build CardAccess.sln
dotnet run --project CardAccess.csproj
dotnet run --project CardAccess.csproj -- --test
```

The last command runs the custom assertion-based test runner. Use it instead of `dotnet test`. The runner prints `FAIL` and returns exit code 1 if a check fails, or exit code 0 if all checks pass. Latest verified result: **15/15 tests passed**.

## Data and business rules

`Program.cs` contains the sample data, so no input files are required.

| Model | Fields |
|---|---|
| `User` | `UserId`: a six-digit ASCII string that preserves leading zeros; `Name`; `Role`; `IsActive` |
| `Card` | `CardHash`; the owner's `UserId`; `IsActive`; `ExpiresAt` |
| `AccessRequest` | `CardId`; `CheckTime`, including its UTC offset |

A `CardId` contains 1-64 ASCII letters, digits or hyphens and is case-sensitive. A `CardHash` contains 64 uppercase hexadecimal characters representing SHA256. Hashing supports record lookup; it does not authenticate a physical card.

User IDs and card hashes must be unique. Input validation rejects unknown roles. A missing request collection is an input error; an empty collection produces an empty result. An activity value of `false` is valid data and causes access denial. The application validates the entire request collection before processing it and rejects the collection if an input has an invalid format.

Access checks run in this order:

1. The card exists.
2. The card is active.
3. The card has not expired.
4. The owner exists.
5. The owner is active.
6. The selected policy allows the owner's role.

If several checks would fail, the application reports the first failure in this order. A card is valid strictly before `ExpiresAt`: `CheckTime >= ExpiresAt` means access is denied.

The **normal policy** allows both `Employee` and `Visitor`. The **restricted policy** allows only `Employee`. Both policies retain the mandatory card and owner checks. In `Program.cs`, the application selects a policy and passes it as a `Func<User, bool>`. `EvaluateAccess` invokes it through `policy(user)`.

## Pipeline

`AccessWorkflow.Run` composes three named stages through successive `Select` calls. Each validly formatted request receives a decision, including requests for unknown cards.

| Stage | Input | Output | Rule and example |
|---|---|---|---|
| `IdentifyCard` | `AccessRequest`, card and user directory snapshots | `Identification` | Hashes `EMP-1` and finds its card and owner `001234`. An unknown card produces no match. |
| `EvaluateAccess` | `Identification`, `Func<User, bool>` | `Evaluation` | Applies mandatory checks and the selected policy. `VIS-1` passes the normal policy and fails the restricted policy. |
| `BuildAccessDecision` | `Evaluation` | `AccessDecision` | Creates a public response containing `UserId`, `CheckedAt`, `Allowed` and `Reason`, without the raw card identifier or hash. |

The models use `sealed record` types with immutable values. Before processing, `Run` copies the input sequences into its own arrays. The stages leave the caller's collections unchanged. Calculations receive the time as an argument and perform no clock reads or console output. The production policies capture no mutable configuration. Console output belongs to `Program.cs`.

The public stage methods expect validated data. Call `Run` as the application entry point for request processing.

## Expected demonstration results

The demonstration uses a fixed check time: **2026-10-11 10:00:00 +05:00**.

The unexpired sample cards expire the next day. `OLD-1` expired one second before the check; `EDGE-1` expires at the exact check time. The application also demonstrates an empty collection, a missing collection and an empty card identifier.

| Card | Normal policy | Restricted policy | Denial reason |
|---|---|---|---|
| `EMP-1` | Allow | Allow | None |
| `VIS-1` | Allow | Deny | `PolicyDenied` under the restricted policy |
| `BLOCK-1` | Deny | Deny | `CardBlocked` |
| `OLD-1` | Deny | Deny | `CardExpired` |
| `EDGE-1` | Deny | Deny | `CardExpired`: exact expiration boundary |
| `UNKNOWN` | Deny | Deny | `CardNotFound` |
| `OFF-1` | Deny | Deny | `UserInactive` |

## Requirement mapping and tests

| Requirement | Implementation |
|---|---|
| One complete operation, sample data and policy selection | `Program.cs` |
| Typed models and input validation | `AccessWorkflow.cs`: records, `ValidateInput`, `GetHash`, `Run` |
| Three pure stages | `IdentifyCard`, `EvaluateAccess`, `BuildAccessDecision` |
| Higher-order function and two policies | `EvaluateAccess`, `NormalPolicy`, `RestrictedPolicy` |
| Automated assertions | `Tests.cs`: `Tests.Run` |

The test runner covers these 15 cases:

1. Complete workflow with expected decisions, owner ID and check time.
2. Empty request collection.
3. Expiration at the exact boundary and one tick before it.
4. Missing request collection.
5. Empty, malformed and missing card identifiers.
6. Different results from both policies for the same visitor.
7. Blocked card.
8. Inactive owner.
9. Unknown card.
10. The `IdentifyCard` stage in isolation.
11. The known SHA256 test vector for `abc`.
12. Unchanged input data and deterministic results.
13. Invalid user ID and duplicate card hash.
14. Missing card owner.
15. Invocation of the supplied policy delegate.

The tests specify expected access outcomes explicitly. A delegate-call counter exists only in a test, not in a production policy.

## Individual defense

Run the demonstration with both policies, then run the tests. Trace `VIS-1` through the three stages and explain each intermediate type. Show the explicit time argument, the expiration check and the delegate invocation.

For a small live change, add an access rule for a new role and update the relevant test.

## Before submission

Add each team member's real name, group and contribution to the logic, tests and documentation. These details require input from the team. Each member must understand and defend their own work. Check the course rules on AI use.

The submission archive contains source files and excludes generated `bin` and `obj` directories.
