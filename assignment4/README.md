# Assignment 4 — ColdChainMonitor

## Run

```bash
dotnet run
```

- `Analysis.cs`: ranges, neighbor comparison, and alert ordering.
- `ArchiveJson.cs`: JSON settings, as well as reading, writing, and content comparison.
- `Program.cs`: FileStream ownership, archive timestamp, and verification of two round trips.
- `ColdChainMonitor.Tests`: mandatory and additional checks.

### 1.Who owns the streams

The program's main method retains ownership of the `FileStream`. It decides when to close the file. In the JSON methods, text wrappers use `leaveOpen: true`: disposing of the wrapper must not deprive the calling code of its stream.

### 2. Why lazy reading via a closed reader is dangerous

The returned `IEnumerable` might hold a reference to the reader rather than the read lines. By the time the calling code begins iteration, the local `using` block will have already finished. Lists and arrays eliminate this dependency if populated within the reader's scope.

### 3. Why reset the `MemoryStream` position

The `MemoryStream` position advances during writing. An attempt to read immediately afterward would start from the end. Before the object is restored, the program explicitly sets `Position = 0`; the stream owner performs this action.

### 4. Why group readings before creating pairs

Adjacent indices are meaningful only within a single sensor's list. First, grouping (ignoring case) and sorting by `Timestamp` are performed; only then does the loop compare `timeline[i - 1]` with `timeline[i]`.

### 5. Why use `Enum.IsDefined` after `Enum.TryParse`

A successful `TryParse` does not prove that the enum actually defines the resulting number. `IsDefined` filters out values ​​like 99. The numbers 0 and 1 correspond to `Cold` and `Frozen` and are not undefined values.

### 6. Where pure operations and I/O are located

String validation, temperature analysis, and archive comparison operate solely on arguments. Import and JSON operations utilize streams. Clock logic and file opening reside in `Program.cs`, allowing calculations to be tested without actual files or the current system time.

### 7. Why the round-trip is verified by value

After reading the JSON, the result is a different archive instance. `ReferenceEquals` does not verify data integrity. Verification requires matching `CreatedAtUtc` values ​​and a sequential comparison of readings, errors, and warnings.

### 8. How ordering and `CreatedAtUtc` aid testing

A fixed timestamp eliminates random discrepancies between test runs. Sorting warnings ensures the result does not depend on the order in which groups are traversed, while the readings array preserves the original import order.

### Fixing the lazy evaluation issue

The reader must not be closed before the deferred `ReadAll` operation completes. You need to read the lines within the `using` block and return a fully populated list or array. This ensures the import loop finishes before `ImportResult` is returned. The test closes the source stream and then iterates over the stored data.

### Fixing MemoryStream reading

Set `stream.Position = 0` before reading the stream that was just written. In the `StreamWriter` implementation, the buffer must be flushed first; `WriteArchive` handles this during the writer's `Dispose` call while keeping the underlying stream open.

### Fixing global neighbor checks

First, group the readings by `SensorId`, then sort each group by time. The index-based loop should operate within the group. For `i = 0`, there is no previous value, so `abrupt` is set to `false`, though the range check is still performed. 
