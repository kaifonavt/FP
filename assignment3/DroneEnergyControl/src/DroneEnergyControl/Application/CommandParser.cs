using System.Globalization;
using DroneEnergyControl.Commands;
using DroneEnergyControl.Domain;

namespace DroneEnergyControl.Application;

public static class CommandParser
{
    private const NumberStyles AmountStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static bool TryParse(string line, out IEnergyCommand? command, out CommandResult? invalidResult)
    {
        command = null;
        invalidResult = null;
        var fields = line.Split('|').Select(x => x.Trim()).ToArray();
        var id = fields.Length > 0 ? fields[0] : null;

        if (fields.Length != 5)
            return Invalid(id, "Each command must contain exactly five fields.", out invalidResult);
        if (string.IsNullOrWhiteSpace(fields[0]))
            return Invalid(null, "Command ID must be non-empty.", out invalidResult);
        if (!TryParseKind(fields[1], out var kind))
            return Invalid(fields[0], "Command kind is not declared.", out invalidResult);
        if (string.IsNullOrWhiteSpace(fields[2]))
            return Invalid(fields[0], "Source ID must be non-empty.", out invalidResult);
        if (!decimal.TryParse(fields[4], AmountStyle, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            return Invalid(fields[0], "Amount must be a positive invariant-culture decimal.", out invalidResult);

        if (kind is CommandKind.Charge or CommandKind.Consume)
        {
            if (fields[3] != "-")
                return Invalid(fields[0], "Charge and Consume require '-' as target ID.", out invalidResult);
            command = kind == CommandKind.Charge
                ? new ChargeCommand(fields[0], fields[2], amount)
                : new ConsumeCommand(fields[0], fields[2], amount);
            return true;
        }

        if (string.IsNullOrWhiteSpace(fields[3]) || fields[3] == "-")
            return Invalid(fields[0], "Transfer requires a non-empty target ID.", out invalidResult);

        command = new TransferCommand(fields[0], fields[2], fields[3], amount);
        return true;
    }

    private static bool TryParseKind(string text, out CommandKind kind)
    {
        foreach (var value in Enum.GetValues<CommandKind>())
        {
            if (string.Equals(value.ToString(), text, StringComparison.OrdinalIgnoreCase))
            {
                kind = value;
                return true;
            }
        }

        kind = default;
        return false;
    }

    private static bool Invalid(string? id, string reason, out CommandResult? result)
    {
        result = CommandResult.Invalid(id, reason);
        return false;
    }
}
