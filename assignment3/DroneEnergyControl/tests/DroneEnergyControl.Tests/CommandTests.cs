using Xunit;
using DroneEnergyControl.Commands;
using DroneEnergyControl.Domain;

namespace DroneEnergyControl.Tests;

public sealed class CommandTests
{
    [Fact]
    public void ChargeCommandAppliesCharge()
    {
        var units = Units(("D1", 100m, 60m));
        var result = new ChargeCommand("C1", "D1", 15m).Execute(units);

        Assert.Equal(ResultClassification.Applied, result.Classification);
        Assert.Equal(75m, units["D1"].Charge);
    }

    [Fact]
    public void ConsumeCommandRejectsInsufficientCharge()
    {
        var units = Units(("D1", 100m, 10m));
        var result = new ConsumeCommand("C1", "D1", 15m).Execute(units);

        Assert.Equal(ResultClassification.Rejected, result.Classification);
        Assert.Equal(10m, units["D1"].Charge);
    }

    [Fact]
    public void TransferCommandAppliesBothChanges()
    {
        var units = Units(("D1", 100m, 60m), ("D2", 80m, 10m));
        var result = new TransferCommand("C1", "D1", "D2", 25m).Execute(units);

        Assert.Equal(ResultClassification.Applied, result.Classification);
        Assert.Equal(35m, units["D1"].Charge);
        Assert.Equal(35m, units["D2"].Charge);
    }

    [Fact]
    public void SameUnitTransferIsRejectedWithoutMutation()
    {
        var units = Units(("D1", 100m, 60m));
        var result = new TransferCommand("C1", "D1", "D1", 5m).Execute(units);

        Assert.Equal(ResultClassification.Rejected, result.Classification);
        Assert.Equal(60m, units["D1"].Charge);
    }

    [Fact]
    public void UnknownUnitIsRejected()
    {
        var result = new ChargeCommand("C1", "UNKNOWN", 5m).Execute(Units());

        Assert.Equal(ResultClassification.Rejected, result.Classification);
    }

    private static Dictionary<string, BatteryUnit> Units(params (string Id, decimal Capacity, decimal Charge)[] values) =>
        values.ToDictionary(x => x.Id, x => new BatteryUnit(x.Id, x.Capacity, x.Charge), StringComparer.OrdinalIgnoreCase);
}