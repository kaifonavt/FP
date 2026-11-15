using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace BuildingSecurity;

public sealed class JsonStore
{
    private readonly string directory;
    private readonly string reportDirectory;

    public JsonStore(string directory, string? reportDirectory = null)
    {
        this.directory = directory;
        this.reportDirectory = reportDirectory ?? Path.Combine(ProjectPaths.Root, "report");
    }

    private static readonly string[] StoreFiles = ["employees.json", "cards.json", "rooms.json", "tickets.json", "reservations.json", "audit.json", "config.json", ".sec-cli.lock"];
    public static JsonSerializerOptions JsonOptions { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        // Every model property is required in JSON: a missing level or
        // status must not silently become 0/false.
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (info.Kind == JsonTypeInfoKind.Object)
                foreach (var property in info.Properties) property.IsRequired = true;
        });
        return new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            TypeInfoResolver = resolver,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
        };
    }

    // Read all seven files, then validate their values and references.
    // Boundary catches file errors at the end of this file.
    public Result<SecurityState> Load()
    {
        return Boundary(() =>
        {
            var employees = Read<ImmutableArray<Employee>>("employees.json");
            var cards = Read<ImmutableArray<Card>>("cards.json");
            var rooms = Read<ImmutableArray<Room>>("rooms.json");
            var tickets = Read<ImmutableArray<Ticket>>("tickets.json");
            var reservations = Read<ImmutableArray<Reservation>>("reservations.json");
            var audit = Read<ImmutableArray<AuditEntry>>("audit.json");
            var config = Read<SecurityConfig>("config.json");

            var state = new SecurityState(employees, cards, rooms, tickets,
                reservations, audit, config);
            return Validation.State(state);
        }, "Loading data");
    }

    private T Read<T>(string name)
    {
        var path = Path.Combine(directory, name);
        var json = File.ReadAllText(path);
        var value = JsonSerializer.Deserialize<T>(json, JsonOptions);

        if (value is null)
            throw new JsonException($"{name}: JSON cannot be null");

        return value;
    }

    // Each workflow changes exactly one file. Multi-file changes are rejected before writing.
    public Result<bool> SaveChange(SecurityState before, SecurityState after) => Validation.State(after).Bind(_ =>
    {
        var changes = new List<(string File, object Value)>();
        if (before.Employees != after.Employees) changes.Add(("employees.json", after.Employees));
        if (before.Cards != after.Cards) changes.Add(("cards.json", after.Cards));
        if (before.Rooms != after.Rooms) changes.Add(("rooms.json", after.Rooms));
        if (before.Tickets != after.Tickets) changes.Add(("tickets.json", after.Tickets));
        if (before.Reservations != after.Reservations) changes.Add(("reservations.json", after.Reservations));
        if (before.Audit != after.Audit) changes.Add(("audit.json", after.Audit));
        if (before.Config != after.Config) changes.Add(("config.json", after.Config));
        return changes.Count switch
        {
            0 => Result<bool>.Success(true),
            1 => Boundary(() => WriteAtomic(Path.Combine(directory, changes[0].File), changes[0].Value), "Saving data"),
            _ => Result<bool>.Failure(ErrorCode.InvalidInput, "An operation must change only one data file")
        };
    });

    public Result<bool> Export<T>(string fileName, T value) => Boundary(() =>
    {
        // Accept a simple JSON filename, never a path to another folder.
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 128 ||
            !char.IsLetterOrDigit(fileName[0]) ||
            !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            fileName.Any(c => !char.IsLetterOrDigit(c) && c is not (' ' or '-' or '_' or '.')))
            return Result<bool>.Failure(ErrorCode.InvalidInput,
                "Use a JSON filename such as access-report.json, without a folder path (up to 128 characters).");
        if (StoreFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            return Result<bool>.Failure(ErrorCode.InvalidInput, "Choose a report name different from the data filenames.");
        if (new DirectoryInfo(reportDirectory).LinkTarget is not null)
            return Result<bool>.Failure(ErrorCode.InvalidInput, "The report folder must be a regular folder, not a link.");

        Directory.CreateDirectory(reportDirectory);
        return WriteAtomic(Path.Combine(reportDirectory, fileName), value);
    }, "Exporting report");

    public Result<FileStream> AcquireLock() => Boundary(() => Result<FileStream>.Success(
        new FileStream(Path.Combine(directory, ".sec-cli.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)),
        "Locking data (check the folder and make sure no other instance is running)");

    private static Result<bool> WriteAtomic<T>(string path, T value)
    {
        string? temporary = null;
        try
        {
            var destination = Path.GetFullPath(path);
            temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, JsonOptions);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, destination, overwrite: true);
            return Result<bool>.Success(true);
        }
        finally
        {
            if (temporary is not null)
                try { File.Delete(temporary); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* Preserve the original write error. */ }
        }
    }

    private static Result<T> Boundary<T>(Func<Result<T>> operation, string context)
    {
        try { return operation(); }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { return Result<T>.Failure(ErrorCode.StorageError, $"{context}: {ex.Message}"); }
    }
}
