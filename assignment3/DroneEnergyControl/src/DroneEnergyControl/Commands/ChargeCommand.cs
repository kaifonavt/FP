using DroneEnergyControl.Domain;

namespace DroneEnergyControl.Commands;

public sealed class ChargeCommand(string commandId, string sourceId, decimal amount) : IEnergyCommand
{
    public string CommandId { get; } = commandId;

    public CommandResult Execute(IReadOnlyDictionary<string, BatteryUnit> units)
    {
        if (!units.TryGetValue(sourceId, out var source))
            return CommandResult.Rejected(CommandId, $"Unknown source unit '{sourceId}'.");

        return source.ChargeBy(amount)
            ? CommandResult.Applied(CommandId, $"{source.Id} charged by {amount}.")
            : CommandResult.Rejected(CommandId, "Charge would exceed capacity.");
    }
}
