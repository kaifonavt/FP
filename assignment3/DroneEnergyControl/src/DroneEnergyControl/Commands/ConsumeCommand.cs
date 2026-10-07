using DroneEnergyControl.Domain;

namespace DroneEnergyControl.Commands;

public sealed class ConsumeCommand(string commandId, string sourceId, decimal amount) : IEnergyCommand
{
    public string CommandId { get; } = commandId;

    public CommandResult Execute(IReadOnlyDictionary<string, BatteryUnit> units)
    {
        if (!units.TryGetValue(sourceId, out var source))
            return CommandResult.Rejected(CommandId, $"Unknown source unit '{sourceId}'.");

        return source.Consume(amount)
            ? CommandResult.Applied(CommandId, $"{source.Id} consumed {amount}.")
            : CommandResult.Rejected(CommandId, "Charge would become negative.");
    }
}
