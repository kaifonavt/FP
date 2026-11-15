using System.Text.Json;
namespace BuildingSecurity;
public sealed class CliSession
{
    private readonly JsonStore store;
    private readonly Func<DateTimeOffset> clock;
    private readonly Func<string> newId;

    public SecurityState State { get; private set; }
    
    public CliSession(JsonStore store, SecurityState state, Func<DateTimeOffset> clock, Func<string> newId)
    {
        this.store = store;
        State = state;
        this.clock = clock;
        this.newId = newId;
    }
    public Result<CommandReply> Execute(string line) => CommandLine.Tokenize(line).Bind(ExecuteTokens);
    public Result<CommandReply> ExecuteTokens(string[] tokens)
    {
        var result = CommandProcessor.Execute(State, tokens, clock(), newId());
        if (result is Result<CommandReply>.Fail)
            return result;

        var reply = ((Result<CommandReply>.Ok)result).Value;
        var saved = store.SaveChange(State, reply.State);
        if (saved is Result<bool>.Fail saveError)
            return new Result<CommandReply>.Fail(saveError.Error);

        if (reply.ExportPath is not null)
        {
            var exported = store.Export(reply.ExportPath, reply.Data);
            if (exported is Result<bool>.Fail exportError)
                return new Result<CommandReply>.Fail(exportError.Error);
        }

        State = reply.State;
        return Result<CommandReply>.Success(reply);
    }

    public int Run(TextReader input, TextWriter output)
    {
        output.WriteLine("Building Security CLI. Type help for commands, exit to quit.");
        while (true)
        {
            output.Write("sec-cli> ");
            var line = input.ReadLine();
            if (line is null) { output.WriteLine(); return 0; }
            var result = Execute(line);
            Print(result, output);
            if (result is Result<CommandReply>.Ok { Value.Exit: true }) return 0;
        }
    }

    public static int Print(Result<CommandReply> result, TextWriter output) => result.Match(reply =>
    {
        if (reply.Message.Length > 0) output.WriteLine(reply.Message);
        if (reply.Data is not null) output.WriteLine(JsonSerializer.Serialize(reply.Data, JsonStore.JsonOptions));
        if (reply.ExportPath is not null) output.WriteLine($"Report saved: report/{reply.ExportPath}");
        return reply.Failed ? 2 : 0;
    }, error => { output.WriteLine($"Error [{error.Code}]: {error.Message}"); return 2; });
}
public static class CliApplication
{
    public static int Run(string[] args, TextReader input, TextWriter output)
    {
        try { return RunApplication(args, input, output); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            output.WriteLine($"I/O error: {ex.Message}");
            return 1;
        }
    }

    private static int RunApplication(string[] args, TextReader input, TextWriter output)
    {
        if (args is ["--help" or "help"]) { output.WriteLine(CommandProcessor.Help); return 0; }
        var directory = Path.Combine(ProjectPaths.Root, "data");
        if (args.Length > 0 && args[0] == "--data")
        {
            if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1])) { output.WriteLine("Specify a folder after --data."); return 1; }
            directory = args[1]; args = args[2..];
        }
        var store = new JsonStore(directory);
        var lockResult = store.AcquireLock();
        if (lockResult is Result<FileStream>.Fail lockError)
        {
            output.WriteLine($"Error [{lockError.Error.Code}]: {lockError.Error.Message}");
            return 1;
        }

        using var handle = ((Result<FileStream>.Ok)lockResult).Value;
        var loaded = store.Load();
        if (loaded is Result<SecurityState>.Fail loadError)
        {
            output.WriteLine($"Error [{loadError.Error.Code}]: {loadError.Error.Message}");
            return 1;
        }

        var state = ((Result<SecurityState>.Ok)loaded).Value;
        var session = new CliSession(store, state,
            () => DateTimeOffset.Now, () => Guid.NewGuid().ToString("N"));

        if (args.Length == 0)
            return session.Run(input, output);

        return CliSession.Print(session.ExecuteTokens(args), output);
    }
}
