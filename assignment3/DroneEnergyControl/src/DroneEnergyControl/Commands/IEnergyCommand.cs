using DroneEnergyControl.Domain;

namespace DroneEnergyControl.Commands;

public interface IEnergyCommand
{
    string CommandId { get; }

    CommandResult Execute(IReadOnlyDictionary<string, BatteryUnit> units);
}
