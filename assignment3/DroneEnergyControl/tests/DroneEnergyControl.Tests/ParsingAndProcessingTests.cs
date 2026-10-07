using Xunit;
using DroneEnergyControl.Application;
using DroneEnergyControl.Domain;

namespace DroneEnergyControl.Tests;

public sealed class ParsingAndProcessingTests
{
    private static readonly string[] RawCommands =
    {
        "C001|Charge|D1|-|15",
        "C002|Consume|D2|-|20",
        "C003|Transfer|D1|D3|25",
        "C004|Transfer|D2|D3|20",
        "C005|99|D1|-|5",
        "c003|Consume|D1|-|10",
        "C006|Transfer|D1|D1|5",
        "C007|Charge|D3|-|-1",
        "C008|Consume|D3|-|35"
    };

    [Fact]
    public void ProvidedLinesProduceExpectedCountsAndCharges()
    {
        var units = InitialUnits();
        var results = CommandProcessor.ExecuteAll(RawCommands, units).ToList();

        Assert.Equal(4, results.Count(x => x.Classification == ResultClassification.Applied));
        Assert.Equal(3, results.Count(x => x.Classification == ResultClassification.Rejected));
        Assert.Equal(2, results.Count(x => x.Classification == ResultClassification.Invalid));
        Assert.Equal(50m, units["D1"].Charge);
        Assert.Equal(50m, units["D2"].Charge);
        Assert.Equal(0m, units["D3"].Charge);
    }

    [Fact]
    public void C004TransferFailsAndLeavesBothD2AndD3Unchanged()
    {
        var units = InitialUnits();
        
        CommandProcessor.ExecuteAll(new[] {
            "C002|Consume|D2|-|20",
            "C003|Transfer|D1|D3|25"
        }, units);

        var d2ChargeBefore = units["D2"].Charge;
        var d3ChargeBefore = units["D3"].Charge;

        var result = CommandProcessor.ExecuteAll(new[] { "C004|Transfer|D2|D3|20" }, units).Single();

        Assert.Equal(ResultClassification.Rejected, result.Classification);
        Assert.Equal(d2ChargeBefore, units["D2"].Charge);
        Assert.Equal(d3ChargeBefore, units["D3"].Charge);
    }

    [Fact]
    public void NumericEnumTextIsInvalid()
    {
        var result = CommandProcessor.ExecuteAll(new[] { "C005|99|D1|-|5" }, InitialUnits()).Single();

        Assert.Equal(ResultClassification.Invalid, result.Classification);
    }

    [Theory]
    [InlineData("C1|Charge|D1|-|0")]
    [InlineData("C2|Charge|D1|-|-1")]
    public void NonPositiveAmountIsInvalid(string line)
    {
        var result = CommandProcessor.ExecuteAll(new[] { line }, InitialUnits()).Single();

        Assert.Equal(ResultClassification.Invalid, result.Classification);
    }

    [Fact]
    public void DuplicateIdIsRejectedBeforeExecutionCaseInsensitively()
    {
        var units = InitialUnits();
        var results = CommandProcessor.ExecuteAll(new[] { "C003|Charge|D1|-|10", "c003|Consume|D1|-|10" }, units).ToList();

        Assert.Equal(ResultClassification.Applied, results[0].Classification);
        Assert.Equal(ResultClassification.Rejected, results[1].Classification);
        Assert.Equal(70m, units["D1"].Charge);
    }

    [Fact]
    public void ExecuteAllIsEagerAndSafeToEnumerateTwice()
    {
        var units = InitialUnits();
        var results = CommandProcessor.ExecuteAll(new[] { "C1|Charge|D1|-|10" }, units);

        _ = results.ToList();
        _ = results.ToList();

        Assert.Equal(70m, units["D1"].Charge);
    }

    [Fact]
    public void FieldsAreTrimmedAndChargeTargetMustBeDash()
    {
        var units = InitialUnits();
        var result = CommandProcessor.ExecuteAll(new[] { " C1 | Charge | D1 | - | 10 " }, units).Single();
        var invalid = CommandProcessor.ExecuteAll(new[] { "C2|Charge|D1|D2|10" }, InitialUnits()).Single();

        Assert.Equal(ResultClassification.Applied, result.Classification);
        Assert.Equal(ResultClassification.Invalid, invalid.Classification);
    }

    [Fact]
    public void MalformedLineIsInvalid()
    {
        var result = CommandProcessor.ExecuteAll(new[] { "C1|Charge|D1|10" }, InitialUnits()).Single();

        Assert.Equal(ResultClassification.Invalid, result.Classification);
    }

    [Fact]
    public void UnknownUnitIsRejectedWithoutThrowing()
    {
        var result = CommandProcessor.ExecuteAll(new[] { "C1|Charge|D9|-|10" }, InitialUnits()).Single();

        Assert.Equal(ResultClassification.Rejected, result.Classification);
    }

    [Fact]
    public void UnrecognizedCommandActionIsInvalid()
    {
        var result = CommandProcessor.ExecuteAll(new[] { "C1|Explode|D1|-|10" }, InitialUnits()).Single();

        Assert.Equal(ResultClassification.Invalid, result.Classification);
    }

    private static Dictionary<string, BatteryUnit> InitialUnits() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["D1"] = new BatteryUnit("D1", 100m, 60m),
        ["D2"] = new BatteryUnit("D2", 80m, 70m),
        ["D3"] = new BatteryUnit("D3", 50m, 10m)
    };
}