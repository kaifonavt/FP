using BuildingSecurity;
using FluentAssertions;
using System.Diagnostics;
using Xunit;

namespace BuildingSecurity.Tests;

public class ReportFolderTests
{
    [Fact]
    public async Task Cli_exports_to_project_report_folder_even_with_custom_data()
    {
        using var temp = new TempStore();
        var project = System.IO.Path.Combine(temp.Path, "BuildingSecuritySystem");
        Directory.CreateDirectory(project);
        File.WriteAllText(System.IO.Path.Combine(project, "BuildingSecuritySystem.sln"), "");
        foreach (var file in new[] { "BuildingSecurity.dll", "BuildingSecurity.deps.json", "BuildingSecurity.runtimeconfig.json" })
            File.Copy(System.IO.Path.Combine(AppContext.BaseDirectory, file), System.IO.Path.Combine(project, file));

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = temp.Path,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { System.IO.Path.Combine(project, "BuildingSecurity.dll"),
                     "--data", temp.Path, "report", "access-summary", "--export", "test report.json" })
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        process.ExitCode.Should().Be(0, await error);
        var expected = System.IO.Path.Combine(project, "report", "test report.json");
        File.Exists(expected).Should().BeTrue("reports belong in the project's report folder");
        File.ReadAllText(expected).Should().Contain("employeeId");
        File.Exists(System.IO.Path.Combine(temp.Path, "test report.json")).Should().BeFalse();
        Directory.Exists(temp.ReportDirectory).Should().BeFalse();
        (await output).Should().Contain("Report saved: report/test report.json");
    }

    [Fact]
    public void Export_rejects_an_absolute_destination_and_preserves_existing_file()
    {
        using var temp = new TempStore();
        var destination = System.IO.Path.Combine(temp.Path, "outside.json");
        File.WriteAllText(destination, "keep this file");
        Fixture.Error(temp.Store.Export(destination, new[] { 1 })).Should().Be(ErrorCode.InvalidInput);
        File.ReadAllText(destination).Should().Be("keep this file");
    }
}
