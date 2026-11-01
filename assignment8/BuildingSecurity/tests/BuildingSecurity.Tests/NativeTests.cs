using Xunit;
using static BuildingSecurity.Tests.TestData;

namespace BuildingSecurity.Tests;

public class NativeTests
{
    [Theory]
    [InlineData(1, 3, 34)]
    [InlineData(0, 3, 0)]
    [InlineData(3, 3, 100)]
    [InlineData(4, 3, 100)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 100)]
    [InlineData(int.MaxValue - 1, int.MaxValue, 100)]
    public void RealNativeCall_ReturnsManuallyExpectedPercent(int count, int capacity, int expected) =>
        Assert.Equal(expected, Ok(new NativeLoadAdapter().Calculate(count, capacity)));

    [Fact]
    public void NegativeInput_IsRejectedBeforeCallingNative()
    {
        int calls = 0;
        var adapter = new NativeLoadAdapter((_, _) => { calls++; return 0; });
        Error(adapter.Calculate(-1, 3), "InvalidLoadInput");
        Error(adapter.Calculate(1, -1), "InvalidLoadInput");
        Assert.Equal(0, calls);
    }

    [Fact]
    public void LoadingAndBindingExceptions_AreResultFailures()
    {
        foreach (var exception in new Exception[] { new DllNotFoundException("missing"), new EntryPointNotFoundException("export"), new BadImageFormatException("architecture") })
        {
            var adapter = new NativeLoadAdapter((_, _) => throw exception);
            Assert.Contains(exception.Message, Error(adapter.Calculate(1, 3), "NativeUnavailable").Message);
        }
    }

    [Fact]
    public void UnexpectedNativeReturn_IsRejected() =>
        Error(new NativeLoadAdapter((_, _) => -1).Calculate(1, 3), "NativeCalculationFailed");

    [Fact]
    public void CompleteWorkflow_LoadsJson_UsesNative_ExportsBothReports()
    {
        var state = ReportTests.Sample();
        var original = state.Visits.ToArray();
        var report = Ok(ReportWorkflow.Build(state, Now, new NativeLoadAdapter()));
        Assert.Equal(new[] { 100, 34 }, report.HostLoad.Select(x => x.LoadPercent));
        Assert.Equal(2, report.Attention.Count);
        Assert.Equal(original, state.Visits);
        var path = Path.Combine(Path.GetTempPath(), $"security-{Guid.NewGuid():N}.json");
        try
        {
            Ok(ReportWorkflow.Export(state, Now, new NativeLoadAdapter(), path));
            using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(34, json.RootElement.GetProperty("HostLoad")[1].GetProperty("LoadPercent").GetInt32());
            Assert.Equal(2, json.RootElement.GetProperty("Attention").GetArrayLength());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void FailedWorkflow_PreservesStateAndExistingExport()
    {
        var state = ReportTests.Sample();
        var visits = state.Visits.ToArray();
        var path = Path.Combine(Path.GetTempPath(), $"security-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "existing export");
            var adapter = new NativeLoadAdapter((_, _) => throw new DllNotFoundException("controlled test"));
            Error(ReportWorkflow.Export(state, Now, adapter, path), "NativeUnavailable");
            Assert.Equal("existing export", File.ReadAllText(path));
            Assert.Equal(visits, state.Visits);
            Assert.Equal(2, Ok(ReportWorkflow.Build(state, Now, new NativeLoadAdapter())).Attention.Count);
        }
        finally { File.Delete(path); }
    }
}
