namespace BuildingSecurity;

public static class Program
{
    private const string Usage =
        "Usage: BuildingSecurity [--demo | --data <path> | --help]";

    private const string Commands = """
        Commands:
          list                  Show guests, passes, rooms and visits
          enter <pass> <room>    Request entry, for example: enter P1 101
          exit <pass>           End an active visit, for example: exit P1
          lookup <guest-id>     Show Some(guest) or None
          count <employee-id>   Show Some(active guest count) or None
          reload <path>         Load a complete new snapshot before any visits
          help                  Show commands
          quit                  End this session (changes are in memory only)
        """;

    // Console I/O and the clock live at this boundary, outside the pure domain pipeline.
    public static int Main(string[] args) =>
        Run(args, Console.In, Console.Out, () => DateTimeOffset.UtcNow);

    public static int Run(
        string[] args,
        TextReader input,
        TextWriter output,
        Func<DateTimeOffset> now)
    {
        if (args is ["--help"])
        {
            output.WriteLine(Usage);
            return 0;
        }

        var demo = args is ["--demo"];
        string path;
        if (args.Length == 0 || demo)
            path = Path.Combine(AppContext.BaseDirectory, "security.json");
        else if (args is ["--data", var suppliedPath])
            path = suppliedPath;
        else
        {
            output.WriteLine(Usage);
            return 2;
        }

        // A failed initial load never starts a session with fabricated/empty data.
        return Parser.Load(path).Match(
            state =>
            {
                output.WriteLine("Building Security - Assignment 7");
                output.WriteLine($"Loaded: {path}");
                if (demo)
                    return RunDemo(state, output);

                output.WriteLine(Commands);
                return RunSession(state, input, output, now, echo: false);
            },
            error =>
            {
                WriteError(output, error);
                return 1;
            });
    }

    private static int RunDemo(SecurityData state, TextWriter output)
    {
        // Sample dates are fixed; the demonstration remains reproducible next year.
        var demoTime = new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
        output.WriteLine($"Demo time: {demoTime:O} (fixed; interactive mode uses real UTC time)");
        var malformed = Path.Combine(AppContext.BaseDirectory, "malformed.json");
        var missing = Path.Combine(AppContext.BaseDirectory, $"missing-{Guid.NewGuid():N}.json");
        using var script = new StringReader(string.Join(Environment.NewLine, new[]
        {
            "lookup 1", "lookup 999", "count 10",
            "enter P1 101",       // Success.
            "enter P2 101",       // Blocked guest.
            "enter P3 101",       // Expired pass.
            "enter P1 102",       // Unauthorized room.
            "enter P4 101",       // Host 20 already hosts P5.
            "enter P999 101",     // Missing required lookup.
            "enter P1 101",       // Duplicate active entry.
            $"reload {malformed}", $"reload {missing}",
            "exit P1", "list", "quit"
        }));
        return RunSession(state, script, output, () => demoTime, echo: true);
    }

    private static int RunSession(
        SecurityData initial,
        TextReader input,
        TextWriter output,
        Func<DateTimeOffset> now,
        bool echo)
    {
        var state = initial;
        while (true)
        {
            output.Write("> ");
            var line = input.ReadLine();
            if (line is null)
                break;
            if (echo)
                output.WriteLine(line);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Handle the final Result once. Only success replaces the session state.
            var stop = Execute(state, line, now).Match(
                outcome =>
                {
                    state = outcome.State;
                    output.WriteLine(outcome.Message);
                    return outcome.Quit;
                },
                error =>
                {
                    WriteError(output, error);
                    return false;
                });
            if (stop)
                break;
        }
        return 0;
    }

    private sealed record ConsoleOutcome(SecurityData State, string Message, bool Quit = false);

    private static Result<ConsoleOutcome, AppError> Execute(
        SecurityData state, string line, Func<DateTimeOffset> now)
    {
        var words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var command = words[0].ToLowerInvariant();

        switch (command)
        {
            case "help" when words.Length == 1:
                return Success(state, Commands);
            case "quit" when words.Length == 1:
                return Result<ConsoleOutcome, AppError>.Success(new(state, "Goodbye.", Quit: true));
            case "list" when words.Length == 1:
                return Success(state, Describe(state));
            case "enter" when words.Length == 3
                                   && int.TryParse(words[2], out var roomId) && roomId > 0:
                return AccessPipeline.ProcessEntry(state, new(words[1], roomId), now())
                    .Map(decision => new ConsoleOutcome(decision.NextState, decision.Message));
            case "exit" when words.Length == 2:
                return AccessPipeline.ProcessExit(state, words[1], now())
                    .Map(decision => new ConsoleOutcome(decision.NextState, decision.Message));
            case "lookup" when words.Length == 2
                                    && int.TryParse(words[1], out var guestId) && guestId > 0:
                return Success(state, AccessPipeline.Find(state.Guests, guest => guest.Id == guestId)
                    .Match(
                        guest => $"Some(guest {guest.Id}): {guest.Name}, blocked={guest.IsBlocked}",
                        () => "None: guest not found"));
            case "count" when words.Length == 2
                                   && int.TryParse(words[1], out var employeeId) && employeeId > 0:
                // A known host with zero active guests is present, not missing.
                var count = AccessPipeline.Find(state.Employees, employee => employee.Id == employeeId)
                    .Match(
                        employee => Option<int>.Some(AccessPipeline.CountActiveGuests(state, employee.Id)),
                        () => Option<int>.None);
                return Success(state, count.Match(
                    value => $"Some({value}): active guests for employee {employeeId}",
                    () => "None: employee not found"));
            case "reload" when words.Length >= 2:
                // Keep the entire remainder so Windows paths may contain spaces.
                var path = line.Trim()[words[0].Length..].Trim();
                if (path.StartsWith('"') && path.EndsWith('"') && path.Length >= 2)
                    path = path[1..^1];
                return Parser.Reload(state, path)
                    .Map(next => new ConsoleOutcome(next, $"Reloaded: {path}"));
            default:
                return Result<ConsoleOutcome, AppError>.Failure(new(
                    "InvalidCommand", "Usage: enter <pass> <positive room>, exit <pass>, lookup <positive guest-id>, " +
                    "count <positive employee-id>, reload <path>, list, help, quit."));
        }
    }

    private static Result<ConsoleOutcome, AppError> Success(SecurityData state, string message) =>
        Result<ConsoleOutcome, AppError>.Success(new(state, message));

    private static void WriteError(TextWriter output, AppError error) =>
        output.WriteLine($"ERROR [{error.Code}]: {error.Message}");

    private static string Describe(SecurityData state)
    {
        var lines = new List<string> { "Guests:" };
        lines.AddRange(state.Guests.Select(g => $"  {g.Id}: {g.Name}, blocked={g.IsBlocked}"));
        lines.Add("Employees:");
        lines.AddRange(state.Employees.Select(e =>
            $"  {e.Id}: {e.Name}, active={AccessPipeline.CountActiveGuests(state, e.Id)}/{e.MaxGuests}"));
        lines.Add("Rooms:");
        lines.AddRange(state.Rooms.Select(r => $"  {r.Id}: {r.Name}, building={r.BuildingId}"));
        lines.Add("Passes:");
        lines.AddRange(state.Passes.Select(p =>
            $"  {p.Id}: guest={p.GuestId}, host={p.EmployeeId}, rooms={string.Join(",", p.AllowedRoomIds)}, " +
            $"valid={p.ValidFrom:O} to {p.ExpiresAt:O}"));
        lines.Add($"Visits: {state.Visits.Count} total, {state.Visits.Count(v => v.IsActive)} active");
        return string.Join(Environment.NewLine, lines);
    }
}
