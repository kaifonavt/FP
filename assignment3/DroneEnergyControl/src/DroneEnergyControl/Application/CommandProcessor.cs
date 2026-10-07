using DroneEnergyControl.Commands;
using DroneEnergyControl.Domain;

namespace DroneEnergyControl.Application;

public static class CommandProcessor
{
    public static IReadOnlyList<CommandResult> ExecuteAll(
        IEnumerable<string> lines,
        IReadOnlyDictionary<string, BatteryUnit> units)
    {
        var results = new List<CommandResult>();
        var processedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (!CommandParser.TryParse(line, out IEnergyCommand? command, out CommandResult? invalidResult))
            {
                results.Add(invalidResult!);
                continue;
            }

            if (!processedIds.Add(command!.CommandId))
            {
                results.Add(CommandResult.Rejected(command.CommandId, "Duplicate command ID."));
                continue;
            }

            results.Add(command.Execute(units));
        }

        return results;
    }
}
