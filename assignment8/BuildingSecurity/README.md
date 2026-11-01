# Building Security System: Guest Access and Reports

**Assignment 8 · Extension Methods and LINQ · IT-2511**

A security officer can record guest entry and exit, see which active visits need attention, and export host capacity figures for reception. This is the C# console from Assignment 7, extended with reports. It retains the access pipeline, validated factories and explicit Result/Option outcomes. The earlier folders remain historical snapshots. For their domain rules, see [the Assignment 7 notes](docs/Assignment7.md).

## Build, run and test

Supported platform: **Windows x64**. Verified with **.NET SDK 8.0.425**, .NET 8 runtime, PowerShell and **MSYS2 UCRT64 GCC 14.2.0**. The first restore needs NuGet access. `global.json` selects SDK 8.0.425 or a later patch in its feature band. Install the MSYS2 package `mingw-w64-ucrt-x86_64-gcc` if GCC is missing.

After extracting the ZIP or opening your local FP repository, run these commands in PowerShell from the directory containing `assignment8`:

```powershell
cd assignment8/BuildingSecurity
powershell -NoProfile -ExecutionPolicy Bypass -File native/build-native.ps1 -Compiler C:/msys64/ucrt64/bin/gcc.exe
dotnet restore BuildingSecurity.sln
dotnet build BuildingSecurity.sln --no-restore --warnaserror
dotnet test BuildingSecurity.sln --no-build --no-restore
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj --no-build -- --report-demo report.json
```

Adjust the compiler path for your installation; omit `-Compiler` if 64-bit MinGW GCC is on PATH. The archive contains C source and the build script. Build C **before** building .NET. The demo prints both reports and writes them to `report.json`; an existing file at that output path is replaced on success. Use a separate filename for exports.

| Location | Contents |
| --- | --- |
| `src/BuildingSecurity/` | Console, domain, pure queries, export and native adapter |
| `tests/BuildingSecurity.Tests/` | Cumulative xUnit suite |
| `sample-data/` | Entry/exit fixture, report fixture and malformed JSON |
| `native/` | C source, header and reproducible DLL build |
| `Directory.Build.props` | x64 target and DLL copy rules for both projects |
| `BuildingSecurity.sln`, `global.json` | Module solution and SDK selection |

## Run in Rider

Open the repository solution, FP.sln, and select **Assignment 8 Reports** to run the fixed-time report demonstration and export report.json. Select **Assignment 8 Interactive** to load the report fixture and enter commands such as reports, list, export, and quit. Both run configurations build the .NET project before launching. Build the native library with the command above after a clean checkout or after editing C source.

For a complete build and test of Assignment 8, use its own `BuildingSecurity.sln` and the commands above. Building every project in the root `FP.sln` also requires the .NET 10 SDK for Assignments 1 and 2. The checked machine has SDKs 8.0.425 and 9.0.305, so that whole-repository build reports NETSDK1045 for those two older projects. Assignment 6 also reports four existing nullable warnings. The Assignment 8 build and its two Rider run configurations pass on .NET 8.

## Reports: what to expect

The parser loads five JSON arrays: guests, employees, rooms, temporary passes and visit history. It rejects missing fields, duplicate identities, broken references, invalid visit times and duplicate active visits. The access pipeline checks the guest, pass, room and host capacity before registering an entry. Entry/exit operations return a new snapshot only on success.

The two reports describe the **current state at a given time**. A timestamp before any recorded entry or exit returns `InvalidReportTime`; the module does not reconstruct past occupancy. Completed visits are excluded. Durations use whole elapsed minutes. The automatic report demo reads `reporting.json` at **20 October 2026, 10:00 UTC**. Interactive reports use current UTC time.

| Report | User, question and inputs | Filter, fields and order |
| --- | --- | --- |
| Attention | Guard: which active visits should I check first? Uses visits, passes, guests and two scoring functions. | Keep active visits lasting at least the supplied minimum minutes (default 0). Show pass, guest ID/name, room, duration and both scores. Sort by night score descending, then ordinal pass ID. |
| Host load | Reception: how many guests is each busy host supervising? Uses active visits, host IDs and host limits. | One row per host with active guests: host ID/name, capacity, count, total minutes, native load percent. Sort by count descending, minutes descending, then numeric host ID. |

The demo has two active visits and one completed visit (P5). P5 contributes to neither report.

| Pass / guest | Room | Minutes | Day score | Night score |
| --- | ---: | ---: | ---: | ---: |
| P4 / Aruzhan | 101 | 90 | 30 | 100 |
| P1 / Ali | 101 | 60 | 0 | 60 |

| Host | Active / capacity | Total minutes | C load |
| --- | ---: | ---: | ---: |
| 20 / Aigerim | 1 / 1 | 90 | 100% |
| 10 / Batyrkhan | 1 / 3 | 60 | 34% |

`SecurityReports.Prepare` joins active visits to validated guest, pass and host records and materialises the shared input. `Attention` uses `Where → Select → OrderByDescending → ThenBy`. `HostTotals` uses `GroupBy`, `Count`, `Sum` and deterministic ordering. `ReportWorkflow` adds the native value after grouping. Calculations do not print or write files. Empty input yields two empty reports; a 1,000-minute attention filter matches neither demo visit.

## Extensions, configurations and lazy processing

| Extension | Receiver → return | Why it belongs there |
| --- | --- | --- |
| `ActiveOnly()` | `IEnumerable<Visit> → IEnumerable<Visit>` | Selects visits by their lifecycle, so callers can compose this filter on any sequence of visits. |
| `WithDuration(asOf)` | `IEnumerable<Visit> → IEnumerable<TimedVisit>` | Adds a duration derived from each visit's entry and exit, or the supplied time for an active visit. |

`Prepare` calls both extensions in one chain. Neither mutates its input. The iterator assumes a valid time; the report boundary checks it before enumeration.

`RiskScoring.CreateScore` is curried: `ScorePolicy → (long minutes → int score)`. Partial application fixes the policy while leaving the visit duration open. `ScorePolicy.Create` validates a nonnegative threshold and a weight of 1..100. The workflow configures both functions and passes them into `BuildConfigured`:

```csharp
var day = RiskScoring.CreateScore(dayPolicy);     // threshold 60, weight 1
var night = RiskScoring.CreateScore(nightPolicy); // threshold 30, weight 2
// Same 90-minute visit: day(90) == 30; night(90) == 100.
```

Score = `min(100, max(0, minutes - threshold) * weight)`. Each closure captures only its immutable policy. No global setting changes. Scores illustrate the assignment; they are not a calibrated security risk model.

`WithDuration` uses `yield return`. Creating the query does no processing. The test-only counter in `ExtensionTests` reads **0** on creation, **1** after a first enumeration and **2** after a second enumeration to an array. Reusing that array leaves it at **2**. Before materialisation, another enumeration repeats pure computation; after `Prepare` calls `ToArray`, both reports can reuse exactly the same rows and clock value.

## Resource and retention notes

- **Read:** `Parser.Load` uses `File.ReadAllText`, which owns and closes its reader before JSON validation starts. Neither extension retains a file handle. Tests obtain exclusive access after successful and malformed loads.
- **Write:** `ReportExporter.Save` owns a temporary sibling file's `FileStream` in a `using` block. It closes the stream before replacing the destination. A failed write or move returns `ReportWriteError`; cleanup runs in `finally`. A locked target keeps its original contents. If the OS also denies cleanup, a temporary file may remain.
- **Console:** the caller owns injected input and output. The built-in demo owns its script reader.
- **Memory:** the parser holds JSON text and DTOs during loading. A deferred query retains its source and lookup dictionaries until materialisation. The report bundle retains only projected result rows. A snapshot costs memory but prevents repeated joins and file reads across the two reports. There are no caches or event subscriptions. Session history can grow until exit; entry and exit replace the validated state.
- **Native:** the function passes and returns integers. There are no native allocations or handles to free. The runtime manages the loaded library.

## Native C contract

`security_load_percent(int32_t active_guests, int32_t capacity) → int32_t`. Both inputs accept 0..2,147,483,647. Negative input returns -1 in C; the managed adapter rejects it first as `InvalidLoadInput`.

| Input | Returned integer percent |
| --- | ---: |
| No guests, including capacity zero | 0 |
| Positive count and zero capacity, or count at/above capacity | 100 |
| Otherwise | Ceiling of 100 × active / capacity |

Manual check: 1 guest / 3 spaces × 100 = 33⅓, so the function returns **34**. C uses 64-bit intermediate arithmetic. The report uses the DLL's value; there is no C# fallback. Host capacity zero is valid in the earlier model, so the percentage rule handles it without division.

`native/security_native.h` declares the Windows export and C calling convention; `native/security_native.c` implements it. `NativeLoadAdapter.cs` binds the exact name with `DllImport`, `ExactSpelling` and `CallingConvention.Cdecl`; C# `int` is a signed 32-bit integer. No unsafe settings are needed.

The build script verifies the MinGW-w64 x64 compiler target, creates the output directory, then runs the equivalent of:

```powershell
gcc -std=c11 -O2 -Wall -Wextra -Werror -shared -static-libgcc native/security_native.c -o runtimes/win-x64/native/security_native.dll
```

The application and xUnit test host load from **different output directories**. A DLL left only in the native build folder cannot satisfy both. The project-level solution is `Directory.Build.props`: it copies the same binary next to both assemblies on build. Rebuild .NET after changing C. A missing DLL, missing export or wrong architecture becomes `NativeUnavailable`; an invalid returned percent becomes `NativeCalculationFailed`.

## Other demonstrations

Run from `assignment8/BuildingSecurity`:

```powershell
# Previous access workflow: successful entry, refusals and exit
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj --no-build -- --demo
# Interactive report fixture; try: reports, export "guard report.json", list, quit
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj --no-build -- --data sample-data/reporting.json
# Safe initial-load failures (exit code 1)
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj --no-build -- --data sample-data/malformed.json
dotnet run --project src/BuildingSecurity/BuildingSecurity.csproj --no-build -- --data sample-data/does-not-exist.json
# Controlled load failure: previous export and visit state survive
dotnet test BuildingSecurity.sln --no-build --filter FullyQualifiedName~FailedWorkflow_PreservesStateAndExistingExport
```

Other console commands include `lookup 1`, `count 10`, `enter P1 101`, `exit P1` and `reload <path>`. An error does not end an interactive session. Exports require an existing parent directory and contain report rows, not a restorable session. The initial JSON remains your data source unless you choose it as the output path.

For a manual loader failure, temporarily move the DLL out of `src/BuildingSecurity/bin/Debug/net8.0`, then run the report demo in a **fresh process** with `--no-build`. Expect `NativeUnavailable`, exit code 1 and no new export. Restore the DLL before running the next demo. Removing a DLL from disk after it has loaded is not a valid failure test.

## Requirements and test evidence

| Task | Implementation | Tests and expected results |
| --- | --- | --- |
| 1. Two custom extensions | `SecurityExtensions.ActiveOnly`, `WithDuration`, both used by `Prepare` | `ExtensionTests`: exclude completed visits; compute 60 whole minutes |
| 2. LINQ reports | `SecurityReports.Attention`, `HostTotals` | `ReportTests`: fixture values above; empty input, no matches, tied keys, two guests totalling 80 minutes, invalid filter/time |
| 3. Curried functions | `RiskScoring.CreateScore`, `ReportWorkflow.BuildConfigured` | Same durations produce 0/60 and 30/100; invalid policies fail |
| 4. Deferred execution | `WithDuration` and `Prepare` | `ExtensionTests`: counter stays 0 before iteration; snapshot reuse does not process again |
| 5–6. Disposal and retention | `Parser.Load`, `ReportExporter.Save`; resource notes above | `ReportFileTests`: exclusive access on success and failure; locked target stays unchanged; missing/malformed input fails |
| 7–8. Native integration | C sources, build script, copy rules, `NativeLoadAdapter.Calculate` | `NativeTests`: real DLL returns 34, edge cases incl. zero capacity; invalid inputs bypass call; binding errors become explicit failures |
| 9. Complete integration | `ReportWorkflow.Build/Export`, `Program.Run` | `NativeTests`, `Assignment8ConsoleTests`: both reports exported, injected native failure preserves existing file and state, quoted paths work, failed export leaves console usable |

The 65 retained tests cover Result/Option, safe model factories, complete access workflows, missing/malformed files and console behaviour. Latest verified result (1 November 2026): **101 passed, 0 failed, 0 skipped**; build with warnings as errors: **0 warnings, 0 errors**. The clean-extraction check builds the native library, restores, builds, runs the suite and exports the report demo.

## Team responsibilities

All four members are in **IT-2511**. The module combines the contributions listed below. Git commits `b55fc31`, `6f7f49f` and `0f4282f` contain the first three members' parts. Batyrkhan's part completes the native C integration, console, Rider run configurations and integration tests.

| Member | Included logic and automated tests | README coverage and defense |
| --- | --- | --- |
| Artur Mikhayevich / `kaifonavt` | Extensions, durations, score policy and currying; `ExtensionTests` | Trace the lazy counter and compare two scores on one visit |
| Meiirzhan Baigurman / `mmeeir` | LINQ queries, grouping and workflow; `ReportTests` | Trace a visit into both reports and explain the tie-breakers |
| Mailybay Zhalgas / `P4fos` | JSON fixtures, parser and export boundary; `ReportFileTests` | Show a failed replacement and prove the old file survived |
| Mukhametzhanov Batyrkhan / `bl1rk` | Native C, binding/build and console; `NativeTests`, `Assignment8ConsoleTests` | Rebuild the DLL, show the 34% call and a controlled failure |

For the final project, keep this runnable module with its C source, build instructions, tests and sample data.
