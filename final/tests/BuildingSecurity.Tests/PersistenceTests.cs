using BuildingSecurity;
using FluentAssertions;
using Xunit;

namespace BuildingSecurity.Tests;

public sealed class TempStore : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sec-cli-test-" + Guid.NewGuid().ToString("N"));
    public string ReportDirectory => System.IO.Path.Combine(Path, "report");
    public JsonStore Store { get; }
    public TempStore(bool sample = true)
    {
        Directory.CreateDirectory(Path);
        Store = new JsonStore(Path, ReportDirectory);
        if (sample) foreach (var file in Directory.GetFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "data"), "*.json"))
                File.Copy(file, System.IO.Path.Combine(Path, System.IO.Path.GetFileName(file)));
    }
    public void Dispose() => Directory.Delete(Path, true);
}

public class PersistenceTests
{
    [Theory]
    [InlineData("../outside.json")]
    [InlineData("..\\outside.json")]
    [InlineData("nested/report.json")]
    [InlineData("nested\\report.json")]
    [InlineData("C:/report.json")]
    [InlineData("/report.json")]
    [InlineData("report.json:extra")]
    [InlineData("report.txt")]
    [InlineData(".json")]
    [InlineData("")]
    [InlineData("report?.json")]
    public void Export_rejects_paths_and_invalid_filenames(string name)
    {
        using var temp = new TempStore();
        Fixture.Error(temp.Store.Export(name, new[] { 1 })).Should().Be(ErrorCode.InvalidInput);
        Directory.Exists(temp.ReportDirectory).Should().BeFalse();
    }

    [Fact]
    public void Export_creates_folder_and_replaces_only_the_named_report()
    {
        using var temp = new TempStore();
        Directory.Exists(temp.ReportDirectory).Should().BeFalse();
        Fixture.Ok(temp.Store.Export("summary.json", new[] { 1 }));
        Fixture.Ok(temp.Store.Export("other.json", new[] { 3 }));
        Fixture.Ok(temp.Store.Export("summary.json", new[] { 2 }));
        File.ReadAllText(System.IO.Path.Combine(temp.ReportDirectory, "summary.json")).Should().Contain("2").And.NotContain("1");
        File.ReadAllText(System.IO.Path.Combine(temp.ReportDirectory, "other.json")).Should().Contain("3");
        Directory.GetFiles(temp.ReportDirectory, "*.tmp").Should().BeEmpty();
        temp.Store.Load().Should().BeOfType<Result<SecurityState>.Ok>();
    }

    [Fact]
    public void Export_failure_preserves_data_and_returns_a_storage_error()
    {
        using var temp = new TempStore();
        File.WriteAllText(temp.ReportDirectory, "A file blocks creation of the report folder");
        Fixture.Error(temp.Store.Export("summary.json", new[] { 1 })).Should().Be(ErrorCode.StorageError);
        temp.Store.Load().Should().BeOfType<Result<SecurityState>.Ok>();
    }

    [Fact]
    public void Missing_file_returns_failure()
    {
        using var temp = new TempStore(false);
        Fixture.Error(temp.Store.Load()).Should().Be(ErrorCode.StorageError);
    }
    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    [InlineData("[{\"id\":\"e1\",\"name\":\"Anna\",\"department\":\"IT\",\"clearanceLevel\":9,\"isActive\":true}]")]
    public void Malformed_or_invalid_data_returns_failure(string json)
    {
        using var temp = new TempStore();
        File.WriteAllText(System.IO.Path.Combine(temp.Path, "employees.json"), json);
        temp.Store.Load().Should().BeOfType<Result<SecurityState>.Fail>();
    }
    [Fact]
    public void Dangling_reference_is_rejected()
    {
        using var temp = new TempStore();
        File.WriteAllText(System.IO.Path.Combine(temp.Path, "cards.json"), "[{\"id\":\"c1\",\"employeeId\":\"absent\",\"isActive\":true}]");
        Fixture.Error(temp.Store.Load()).Should().Be(ErrorCode.InvalidInput);
    }
    [Fact]
    public void Scan_round_trip_persists_denial_and_preserves_other_files()
    {
        using var temp = new TempStore();
        var before = Fixture.Ok(temp.Store.Load());
        var originalEmployees = File.ReadAllText(System.IO.Path.Combine(temp.Path, "employees.json"));
        var after = Access.Scan(before, "unknown", "r1", Fixture.Noon, "new-audit").State;
        Fixture.Ok(temp.Store.SaveChange(before, after)).Should().BeTrue();
        Fixture.Ok(temp.Store.Load()).Audit.Last().Granted.Should().BeFalse();
        File.ReadAllText(System.IO.Path.Combine(temp.Path, "employees.json")).Should().Be(originalEmployees);
    }
    [Fact]
    public void Failed_write_returns_failure_without_touching_other_files()
    {
        using var temp = new TempStore();
        var before = Fixture.Ok(temp.Store.Load());
        var path = System.IO.Path.Combine(temp.Path, "audit.json");
        File.Delete(path); Directory.CreateDirectory(path);
        var after = Access.Scan(before, "c1", "r1", Fixture.Noon, "new-audit").State;
        Fixture.Error(temp.Store.SaveChange(before, after)).Should().Be(ErrorCode.StorageError);
        Directory.GetFiles(temp.Path, "*.tmp").Should().BeEmpty();
    }
    [Fact]
    public void Report_export_is_json_and_cannot_overwrite_store()
    {
        using var temp = new TempStore();
        var state = Fixture.Ok(temp.Store.Load());
        var path = System.IO.Path.Combine(temp.ReportDirectory, "report with spaces.json");
        Fixture.Ok(temp.Store.Export("report with spaces.json", Reports.AccessSummary(state))).Should().BeTrue();
        File.ReadAllText(path).Should().StartWith("[");
        Fixture.Error(temp.Store.Export("employees.json", new[] { 1 })).Should().Be(ErrorCode.InvalidInput);
    }
    [Fact]
    public void Second_process_cannot_lock_same_store()
    {
        using var temp = new TempStore();
        using var handle = Fixture.Ok(temp.Store.AcquireLock());
        Fixture.Error(temp.Store.AcquireLock()).Should().Be(ErrorCode.StorageError);
    }
}
