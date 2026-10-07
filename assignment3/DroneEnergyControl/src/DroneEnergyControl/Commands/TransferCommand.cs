using DroneEnergyControl.Domain;

namespace DroneEnergyControl.Commands;

public sealed class TransferCommand(string commandId, string sourceId, string targetId, decimal amount) : IEnergyCommand
{
    public string CommandId { get; } = commandId;

    public CommandResult Execute(IReadOnlyDictionary<string, BatteryUnit> units)
    {
        if (!units.TryGetValue(sourceId, out var source))
            return CommandResult.Rejected(CommandId, $"Unknown source unit '{sourceId}'.");
        if (!units.TryGetValue(targetId, out var target))
            return CommandResult.Rejected(CommandId, $"Unknown target unit '{targetId}'.");
        if (source.TransferTo(target, amount))
            return CommandResult.Applied(CommandId, $"Transferred {amount} from {source.Id} to {target.Id}.");

        return CommandResult.Rejected(CommandId, "Transfer would violate balance, capacity, or same-unit rules.");
    }
}
