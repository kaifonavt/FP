namespace BuildingSecurity;
public sealed record CommandReply(SecurityState State, string Message, object? Data = null, bool Exit = false, string? ExportPath = null, bool Failed = false);
public static class CommandProcessor
{
    public const string Help = """
        Building Security CLI — manage building security
        Usage: dotnet run --project src/BuildingSecurity -- [--data <directory>] [command ...]
        Without a command, the CLI starts in interactive mode.
        Data defaults to the project's data folder. Use --data to choose another directory.

        scan-card <card_id> <room_id>
        support-ticket list [--status <open|closed>]
        support-ticket create <employee_id> <description>
        support-ticket resolve <ticket_id>
        reservation book <room_id> <employee_id> <start_time> <end_time>
        reservation cancel <reservation_id>
        reservation list <room_id>
        audit-log stream [--employee <id>] [--room <id>] [--denied-only]
        audit-log anomalies [--threshold <count>]
        report access-summary [--export <file_name>]
        report room-utilization [--export <file_name>]
        config show
        config set-mode <normal|night|lockdown>
        config set-hours <open_time> <close_time>
        help | exit | quit

        Dates: 2026-11-13T10:00:00+05:00 or 2026-11-13T05:00:00Z.
        Hours: HH:mm. Quote descriptions and paths that contain spaces.
        IDs: letters, digits, periods, hyphens, and underscores (up to 128 characters).
        Export accepts only a JSON filename and saves it in the project's report folder.
        Examples: scan-card c1 r2; support-ticket create e1 "Lock is broken"
        """;

    private static Result<CommandReply> Bad(string message) => Result<CommandReply>.Failure(ErrorCode.InvalidInput, message);
    private static Result<CommandReply> Reply(SecurityState state, string message, object? data = null) =>
        Result<CommandReply>.Success(new(state, message, data));
    
    public static Result<CommandReply> Execute(SecurityState state, string[] tokens, DateTimeOffset now, string id)
    {
        if (tokens.Length == 0)
            return Reply(state, "");

        var command = tokens[0];
        var arguments = tokens.Skip(1).ToArray();

        switch (command)
        {
            case "help":
            case "--help":
                if (arguments.Length == 0)
                    return Reply(state, Help);
                break;
            case "exit":
            case "quit":
                if (arguments.Length == 0)
                    return Result<CommandReply>.Success(new(state, "Goodbye.", Exit: true));
                break;
            case "scan-card":
                if (arguments.Length == 2 && Validation.ValidId(arguments[0]) && Validation.ValidId(arguments[1]))
                    return Scan(state, arguments[0], arguments[1], now, id);
                break;
            case "support-ticket":
                return Support(state, arguments, now, id);
            case "reservation":
                return Reservation(state, arguments, id);
            case "audit-log":
                return Audit(state, arguments);
            case "report":
                if (arguments.Length > 0)
                    return Report(state, arguments[0], arguments.Skip(1).ToArray());
                break;
            case "config":
                return Config(state, arguments);
        }

        return Bad("Unknown command or invalid arguments. Type help.");
    }

    private static Result<CommandReply> Scan(SecurityState state, string card, string room, DateTimeOffset now, string id)
    {
        var outcome = Access.Scan(state, card, room, now, id);
        return Result<CommandReply>.Success(outcome.Decision.Match(
            d => new CommandReply(outcome.State, d.Reason, outcome.State.Audit[^1]),
            e => new CommandReply(outcome.State, $"DENIED [{e.Code}]: {e.Message}", outcome.State.Audit[^1], Failed: true)));
    }

    private static Result<CommandReply> Support(SecurityState state, string[] args, DateTimeOffset now, string id) => args switch
    {
        ["list", .. var options] => CommandLine.Options(options, "--status").Bind(o =>
            o.GetValueOrDefault("--status") is null or "open" or "closed"
                ? Reply(state, "Tickets:", state.Tickets.Where(t => !o.ContainsKey("--status") ||
                    t.Status == (o["--status"] == "open" ? TicketStatus.Open : TicketStatus.Closed)).OrderBy(t => t.CreatedAt).ThenBy(t => t.Id, StringComparer.Ordinal).ToArray())
                : Bad("Status must be open or closed")),
        ["create", var employee, .. var description] when description.Length > 0 =>
            Tickets.Create(state, employee, string.Join(" ", description), now, id).Map(s => new CommandReply(s, $"Ticket created: {id}", s.Tickets[^1])),
        ["resolve", var ticket] => Tickets.Resolve(state, ticket, now).Map(s => new CommandReply(s, $"Ticket closed: {ticket}")),
        _ => Bad("Usage: support-ticket list|create|resolve (see help)")
    };

    private static Result<CommandReply> Reservation(SecurityState state, string[] args, string id) => args switch
    {
        ["book", var room, var employee, var start, var end] when CommandLine.Timestamp(start, out var from) && CommandLine.Timestamp(end, out var to) =>
            Reservations.Book(state, room, employee, from, to, id).Map(s => new CommandReply(s, $"Reservation created: {id}", s.Reservations[^1])),
        ["cancel", var reservation] => Reservations.Cancel(state, reservation).Map(s => new CommandReply(s, $"Reservation cancelled: {reservation}")),
        ["list", var room] => state.Rooms.FindEntity(r => r.Id == room, $"room {room}")
            .Map(_ => new CommandReply(state, "Reservations:", state.Reservations.Where(r => r.RoomId == room)
                .OrderBy(r => r.Start).ThenBy(r => r.Id, StringComparer.Ordinal).ToArray())),
        _ => Bad("Usage: reservation book|cancel|list. ISO 8601 dates must include Z or an offset such as +05:00.")
    };

    private static Result<CommandReply> Audit(SecurityState state, string[] args) => args switch
    {
        ["stream", .. var options] => CommandLine.Options(options, "--employee", "--room", "--denied-only")
            .Map(o => new CommandReply(state, "Events:", state.Audit.StreamWhere(
                Reports.AuditFilter(o.GetValueOrDefault("--employee"))(o.GetValueOrDefault("--room"))(o.ContainsKey("--denied-only"))).ToList())),
        ["anomalies", .. var options] => CommandLine.Options(options, "--threshold").Bind(o =>
            int.TryParse(o.GetValueOrDefault("--threshold", "3"), out var threshold) && threshold > 0
                ? Reply(state, $"Anomalies (denials >= {threshold} across the entire log):", Reports.Anomalies(state.Audit, threshold))
                : Bad("Threshold must be a positive integer")),
        _ => Bad("Usage: audit-log stream|anomalies (see help)")
    };

    private static Result<CommandReply> Report(SecurityState state, string report, string[] options) =>
        CommandLine.Options(options, "--export").Bind(o => report switch
        {
            "access-summary" => Result<CommandReply>.Success(new(state, "Access summary:", Reports.AccessSummary(state), ExportPath: o.GetValueOrDefault("--export"))),
            "room-utilization" => Result<CommandReply>.Success(new(state, "Room utilization:", Reports.RoomUtilization(state), ExportPath: o.GetValueOrDefault("--export"))),
            _ => Bad("Report must be access-summary or room-utilization")
        });

    private static Result<CommandReply> Config(SecurityState state, string[] args) => args switch
    {
        ["show"] => Reply(state, "Configuration:", state.Config),
        ["set-mode", var mode] when mode is "normal" or "night" or "lockdown" => UpdateConfig(state, state.Config with
        {
            Mode = mode switch { "normal" => BuildingMode.Normal, "night" => BuildingMode.Night, _ => BuildingMode.Lockdown }
        }),
        ["set-hours", var open, var close] when CommandLine.Hours(open, out var from) && CommandLine.Hours(close, out var to) =>
            UpdateConfig(state, state.Config with { OpenTime = from, CloseTime = to }),
        _ => Bad("Usage: config show | set-mode normal|night|lockdown | set-hours HH:mm HH:mm")
    };

    private static Result<CommandReply> UpdateConfig(SecurityState state, SecurityConfig config) =>
        Validation.Config(config).Map(c => new CommandReply(state with { Config = c }, "Configuration updated:", c));
}
