using Xunit;
using static BuildingSecurity.Tests.TestData;

namespace BuildingSecurity.Tests;

public class Assignment8ConsoleTests
{
    [Fact]
    public void ReportDemo_UsesFixedTimeAndExportsActualNativeValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"report demo-{Guid.NewGuid():N}.json");
        try
        {
            using var output = new StringWriter();
            Assert.Equal(0, Program.Run(new[] { "--report-demo", path }, TextReader.Null, output,
                () => Now.AddYears(20)));
            Assert.Contains("day=30, night=100", output.ToString());
            Assert.Contains("native load=34%", output.ToString());
            Assert.True(File.Exists(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void FailedExportCommand_DoesNotBreakSessionOrChangeVisitHistory()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), $"absent-{Guid.NewGuid():N}", "report.json");
        using var input = new StringReader($"export \"{missingDirectory}\"\nlookup 1\nlist\nquit\n");
        using var output = new StringWriter();
        Assert.Equal(0, Program.Run(Array.Empty<string>(), input, output, () => Now));
        Assert.Contains("ERROR [ReportWriteError]", output.ToString());
        Assert.Contains("Some(guest 1)", output.ToString());
        Assert.Contains("Visits: 1 total, 1 active", output.ToString());
        Assert.False(File.Exists(missingDirectory));
    }

    [Fact]
    public void ExportCommand_AcceptsQuotedPathWithSpaces()
    {
        var path = Path.Combine(Path.GetTempPath(), $"security report-{Guid.NewGuid():N}.json");
        try
        {
            using var input = new StringReader($"export \"{path}\"\nquit\n");
            using var output = new StringWriter();
            Assert.Equal(0, Program.Run(Array.Empty<string>(), input, output, () => Now));
            Assert.True(File.Exists(path));
            Assert.DoesNotContain("ERROR", output.ToString());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReportsCommand_DisplaysBothReports_AndKeepsSessionUsable()
    {
        using var input = new StringReader("reports\nlookup 1\nquit\n");
        using var output = new StringWriter();
        Assert.Equal(0, Program.Run(Array.Empty<string>(), input, output, () => Now));
        Assert.Contains("Attention report", output.ToString());
        Assert.Contains("Host load report", output.ToString());
        Assert.Contains("Some(guest 1)", output.ToString());
        Assert.DoesNotContain("ERROR", output.ToString());
    }
}
