using Xunit;
using static BuildingSecurity.Tests.TestData;

namespace BuildingSecurity.Tests;

public class ReportFileTests
{
    [Fact]
    public void Export_ReleasesFileAfterSuccess_AndWritesReadableJson()
    {
        var path = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.json");
        try
        {
            var report = Ok(ReportWorkflow.Build(ReportTests.Sample(), Now, new NativeLoadAdapter()));
            Ok(ReportExporter.Save(report, path));
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using var json = System.Text.Json.JsonDocument.Parse(exclusive);
            Assert.Equal(2, json.RootElement.GetProperty("HostLoad").GetArrayLength());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void FailedExport_ReleasesTemporaryFileAndPreservesLockedTarget()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "report.json");
        try
        {
            File.WriteAllText(path, "original");
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var report = Ok(ReportWorkflow.Build(ReportTests.Sample(), Now, new NativeLoadAdapter()));
                Error(ReportExporter.Save(report, path), "ReportWriteError");
                Assert.Single(Directory.GetFiles(directory));
            }
            Assert.Equal("original", File.ReadAllText(path));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("security.json", true)]
    [InlineData("malformed.json", false)]
    public void Parser_ReleasesFileOnSuccessAndMalformedInput(string fixture, bool valid)
    {
        var path = Path.Combine(Path.GetTempPath(), $"input-{Guid.NewGuid():N}.json");
        try
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, fixture), path);
            if (valid) Ok(Parser.Load(path)); else Error(Parser.Load(path), "MalformedFile");
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.Length > 0);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MissingInput_IsFailureAndCreatesNoFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"absent-{Guid.NewGuid():N}.json");
        Error(Parser.Load(path), "FileNotFound");
        Assert.False(File.Exists(path));
    }
}
