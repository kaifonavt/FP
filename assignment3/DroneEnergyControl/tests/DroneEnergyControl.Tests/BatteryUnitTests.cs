using Xunit;
using DroneEnergyControl.Domain;

namespace DroneEnergyControl.Tests;

public sealed class BatteryUnitTests
{
    [Fact]
    public void ChargingExactlyToCapacitySucceeds()
    {
        var unit = new BatteryUnit("D1", 100m, 60m);

        Assert.True(unit.ChargeBy(40m));
        Assert.Equal(100m, unit.Charge);
    }

    [Fact]
    public void ConsumingExactlyToZeroSucceeds()
    {
        var unit = new BatteryUnit("D1", 100m, 40m);

        Assert.True(unit.Consume(40m));
        Assert.Equal(0m, unit.Charge);
    }

    [Fact]
    public void FailedTransferPreservesBothCharges()
    {
        var source = new BatteryUnit("D2", 80m, 70m);
        var target = new BatteryUnit("D3", 50m, 40m);
        var sourceBefore = source.Charge;
        var targetBefore = target.Charge;

        Assert.False(source.TransferTo(target, 20m));
        Assert.Equal(sourceBefore, source.Charge);
        Assert.Equal(targetBefore, target.Charge);
    }

    [Fact]
    public void ConstructorRejectsInvalidState()
    {
        Assert.Throws<ArgumentException>(() => new BatteryUnit("", 10m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryUnit("D1", 0m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryUnit("D1", 10m, 11m));
    }

    [Fact]
    public void ChargeBeyondCapacityIsRejectedWithoutPartialApplication()
    {
        var unit = new BatteryUnit("D1", 100m, 90m);
        
        Assert.False(unit.ChargeBy(15m));
        Assert.Equal(90m, unit.Charge); 
    }

    [Fact]
    public void ConsumeMoreThanAvailableChargeIsRejected()
    {
        var unit = new BatteryUnit("D1", 100m, 10m);
        
        Assert.False(unit.Consume(15m));
        Assert.Equal(10m, unit.Charge); 
    }
}
