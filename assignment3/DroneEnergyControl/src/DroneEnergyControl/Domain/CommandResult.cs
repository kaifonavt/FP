namespace DroneEnergyControl.Domain;

public enum ResultClassification
{
    Applied,
    Rejected,
    Invalid
}

public sealed record CommandResult(
    string? CommandId,
    ResultClassification Classification,
    string Reason)
{
    public static CommandResult Applied(string commandId, string reason) =>
        new(commandId, ResultClassification.Applied, reason);

    public static CommandResult Rejected(string commandId, string reason) =>
        new(commandId, ResultClassification.Rejected, reason);

    public static CommandResult Invalid(string? commandId, string reason) =>
        new(commandId, ResultClassification.Invalid, reason);
}
