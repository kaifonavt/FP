using Xunit;
using static BuildingSecurity.Tests.TestData;

namespace BuildingSecurity.Tests;

public class ConsoleTests
{
    [Fact]
    public void Demo_UsesDocumentedTimeEvenWhenRealClockIsPastSampleExpiry()
    {
        using var output = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "--demo" }, TextReader.Null, output,
            () => DateTimeOffset.Parse("2040-01-01T00:00:00Z")));
        Assert.Contains("Demo time: 2026-10-20", output.ToString());
        Assert.Contains("GRANTED:", output.ToString());
    }

    [Fact]
    public void EmptyLinesAndEndOfInput_EndSessionWithoutAnError()
    {
        using var files = new FileTests();
        using var input = new StringReader("\n  \nhelp\n");
        using var output = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "--data", files.Write(FileTests.ValidJson) }, input, output, () => Now));
        Assert.Contains("Commands:", output.ToString());
        Assert.DoesNotContain("ERROR", output.ToString());
    }

    [Fact]
    public void LookupAndCount_DistinguishPresentFalseAndZeroFromMissingRecords()
    {
        using var files = new FileTests();
        using var input = new StringReader("lookup 1\nlookup 999\ncount 10\ncount 999\nquit\n");
        using var output = new StringWriter();
        Program.Run(new[] { "--data", files.Write(FileTests.ValidJson) }, input, output, () => Now);
        var text = output.ToString();
        Assert.Contains("Some(guest 1)", text);
        Assert.Contains("blocked=False", text);
        Assert.Contains("None: guest not found", text);
        Assert.Contains("Some(0)", text);
        Assert.Contains("None: employee not found", text);
    }

    [Fact]
    public void Reload_HandlesQuotedPathWithSpacesAndAppliesNewState()
    {
        using var files = new FileTests();
        var initial = files.Write(FileTests.ValidJson);
        var path = Path.Combine(Path.GetDirectoryName(initial)!, "new data.json");
        File.WriteAllText(path, FileTests.ValidJson.Replace("\"maxGuests\":0", "\"maxGuests\":1"));
        using var input = new StringReader($"reload \"{path}\"\nenter P1 101\nquit\n");
        using var output = new StringWriter();
        Program.Run(new[] { "--data", initial }, input, output, () => Now);
        Assert.Contains("Reloaded:", output.ToString());
        Assert.Contains("GRANTED:", output.ToString());
    }

    [Fact]
    public void ExtraArguments_AreNotSilentlyIgnored()
    {
        using var output = new StringWriter();
        Assert.Equal(2, Program.Run(new[] { "--demo", "extra" }, TextReader.Null, output, () => Now));
        Assert.Contains("Usage:", output.ToString());
    }

    [Fact]
    public void InitialMissingFile_ReturnsFailureWithoutStartingSession()
    {
        using var output = new StringWriter();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        Assert.Equal(1, Program.Run(new[] { "--data", path }, TextReader.Null, output, () => Now));
        Assert.Contains("FileNotFound", output.ToString());
        Assert.DoesNotContain("Commands:", output.ToString());
    }

    [Fact]
    public void BundledSamples_AreAvailableFromOutputDirectory()
    {
        var state = Ok(Parser.Load(Path.Combine(AppContext.BaseDirectory, "security.json")));
        Assert.Equal(5, state.Guests.Count);
        Error(Parser.Load(Path.Combine(AppContext.BaseDirectory, "malformed.json")), "MalformedFile");
    }

    [Fact]
    public void Help_DoesNotLoadDataOrStartSession()
    {
        using var output = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "--help" }, TextReader.Null, output, () => Now));
        Assert.Contains("Usage:", output.ToString());
        Assert.DoesNotContain("Loaded:", output.ToString());
    }

    [Fact]
    public void ShippedDemo_ShowsAllRequiredOutcomesAndPreservesHistoryAfterFileFailures()
    {
        using var output = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "--demo" }, TextReader.Null, output, () => Now));
        var text = output.ToString();
        foreach (var expected in new[] { "GRANTED:", "GuestBlocked", "PassExpired", "ZoneAccessDenied",
            "HostGuestLimitExceeded", "PassNotFound", "Some(guest 1)", "None: guest not found",
            "Some(0)", "EXIT:", "MalformedFile", "FileNotFound", "Visits: 2 total, 1 active" })
            Assert.Contains(expected, text);
    }

    [Fact]
    public void InteractiveEntryFailureExit_KeepCapacityConsistent()
    {
        using var files = new FileTests();
        var path = files.Write(FileTests.ValidJson.Replace("\"maxGuests\":0", "\"maxGuests\":1"));
        using var input = new StringReader("enter P1 101\nenter P1 101\nexit P1\nenter P1 101\nlist\nquit\n");
        using var output = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "--data", path }, input, output, () => Now));
        var text = output.ToString();
        Assert.Equal(2, text.Split("GRANTED:").Length - 1);
        Assert.Contains("AlreadyInside", text);
        Assert.Contains("EXIT:", text);
        Assert.Contains("Visits: 2 total, 1 active", text);
    }

    [Theory]
    [InlineData("bad command\nenter P1 abc\nenter P1\n", "Usage:")]
    [InlineData("enter P999 101\n", "PassNotFound")]
    [InlineData("enter P1 102\n", "RoomNotFound")]
    [InlineData("enter P1 101\n", "HostGuestLimitExceeded")]
    public void InvalidCommandsAndBusinessRefusals_DoNotCrash(string commands, string expected)
    {
        using var files = new FileTests();
        using var input = new StringReader(commands);
        using var output = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "--data", files.Write(FileTests.ValidJson) }, input, output, () => Now));
        Assert.Contains(expected, output.ToString());
    }

    [Fact]
    public void FailedReload_PreservesUsableStateInActualConsole()
    {
        using var files = new FileTests();
        var valid = files.Write(FileTests.ValidJson.Replace("\"maxGuests\":0", "\"maxGuests\":1"));
        var invalid = files.Write("{ broken");
        using var input = new StringReader($"reload {invalid}\nenter P1 101\nreload {valid}\nlist\nquit\n");
        using var output = new StringWriter();
        Assert.Equal(0, Program.Run(new[] { "--data", valid }, input, output, () => Now));
        Assert.Contains("MalformedFile", output.ToString());
        Assert.Contains("GRANTED:", output.ToString());
        Assert.Contains("ReloadWouldDiscardVisits", output.ToString());
        Assert.Contains("Visits: 1 total, 1 active", output.ToString());
    }

    [Fact]
    public void InitialFileFailure_ReturnsNonzeroWithoutStartingSession()
    {
        using var files = new FileTests();
        using var output = new StringWriter();
        Assert.Equal(1, Program.Run(new[] { "--data", files.Write("{ broken") }, TextReader.Null, output, () => Now));
        Assert.Contains("MalformedFile", output.ToString());
        Assert.DoesNotContain("Commands:", output.ToString());
    }

    [Theory]
    [InlineData("--data")]
    [InlineData("--unknown")]
    public void InvalidArguments_ReturnUsageError(string argument)
    {
        using var output = new StringWriter();
        Assert.Equal(2, Program.Run(new[] { argument }, TextReader.Null, output, () => Now));
        Assert.Contains("Usage:", output.ToString());
    }
}
