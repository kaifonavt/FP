using BuildingSecurity;
using FluentAssertions;
using Xunit;

namespace BuildingSecurity.Tests;

public class CliTests
{
    [Fact]
    public void Tokenizer_preserves_quoted_descriptions_and_report_filenames()
    {
        Fixture.Ok(CommandLine.Tokenize("support-ticket create e1 \"The lock is broken\""))
            .Should().Equal("support-ticket", "create", "e1", "The lock is broken");
        Fixture.Ok(CommandLine.Tokenize("report access-summary --export \"access report.json\""))
            .Last().Should().Be("access report.json");
        Fixture.Error(CommandLine.Tokenize("\"unclosed")).Should().Be(ErrorCode.InvalidInput);
    }
    [Theory]
    [InlineData("scan-card")]
    [InlineData("scan-card c1 r1 extra")]
    [InlineData("config set-mode 1")]
    [InlineData("config set-hours 08:00 08:00")]
    [InlineData("config set-hours 25:00 18:00")]
    [InlineData("reservation book r1 e1 yesterday tomorrow")]
    [InlineData("reservation book r1 e1 2026-11-12T10:00:00 2026-11-12T11:00:00")]
    [InlineData("audit-log anomalies --threshold 0")]
    [InlineData("audit-log stream --unknown")]
    [InlineData("audit-log stream --room r1 --room r2")]
    [InlineData("report access-summary --export")]
    [InlineData("support-ticket list --status nonsense")]
    [InlineData("unknown")]
    public void Invalid_commands_fail_without_state_changes(string command)
    {
        var state = Fixture.State;
        var result = CommandProcessor.Execute(state, Fixture.Ok(CommandLine.Tokenize(command)), Fixture.Noon, "new-id");
        result.Should().BeOfType<Result<CommandReply>.Fail>();
        state.Audit.Should().BeEmpty(); state.Tickets.Should().BeEmpty();
    }
    [Fact]
    public void Booking_workflow_rejects_conflict_then_cancels_and_exports_report()
    {
        using var temp = new TempStore();
        var counter = 0;
        var session = new CliSession(temp.Store, Fixture.Ok(temp.Store.Load()), () => Fixture.Noon, () => "new-" + ++counter);
        Fixture.Ok(session.Execute("reservation book r1 e2 2026-11-14T10:00:00+05:00 2026-11-14T11:00:00+05:00"));
        Fixture.Error(session.Execute("reservation book r1 e2 2026-11-14T10:30:00+05:00 2026-11-14T11:00:00+05:00"))
            .Should().Be(ErrorCode.TimeSlotConflict);
        Fixture.Ok(session.Execute("reservation cancel new-1"));
        var export = System.IO.Path.Combine(temp.ReportDirectory, "room report.json");
        Fixture.Ok(session.Execute("report room-utilization --export \"room report.json\""));
        File.ReadAllText(export).Should().Contain("totalHours");
        Fixture.Ok(temp.Store.Load()).Reservations.Last().Status.Should().Be(ReservationStatus.Cancelled);
    }
    [Fact]
    public void Ticket_and_config_commands_persist_and_reject_invalid_transition()
    {
        using var temp = new TempStore();
        var counter = 0;
        var session = new CliSession(temp.Store, Fixture.Ok(temp.Store.Load()), () => Fixture.Noon, () => "new-" + ++counter);
        Fixture.Ok(session.Execute("support-ticket create e1 \"Broken lock\""));
        Fixture.Ok(session.Execute("support-ticket resolve new-1"));
        Fixture.Error(session.Execute("support-ticket resolve new-1")).Should().Be(ErrorCode.TicketAlreadyResolved);
        Fixture.Ok(session.Execute("support-ticket list --status closed")).Data.Should().BeOfType<Ticket[]>().Which.Should().HaveCount(2);
        Fixture.Ok(session.Execute("config set-mode lockdown"));
        Fixture.Ok(session.Execute("config set-hours 22:00 06:00"));
        var config = Fixture.Ok(temp.Store.Load()).Config;
        config.Mode.Should().Be(BuildingMode.Lockdown); config.OpenTime.Should().Be(TimeSpan.FromHours(22));
        Fixture.Ok(session.Execute("config show")).Data.Should().Be(config);
    }
    [Fact]
    public void Audit_filters_and_threshold_use_current_data()
    {
        using var temp = new TempStore();
        var session = new CliSession(temp.Store, Fixture.Ok(temp.Store.Load()), () => Fixture.Noon, () => "new-id");
        Fixture.Ok(session.Execute("audit-log stream --employee e2 --room r2 --denied-only"))
            .Data.Should().BeOfType<List<AuditEntry>>().Which.Should().HaveCount(3);
        Fixture.Ok(session.Execute("audit-log anomalies --threshold 3"))
            .Data.Should().BeOfType<AnomalyRow[]>().Which.Should().ContainSingle().Which.Denied.Should().Be(3);
        Fixture.Ok(session.Execute("reservation list r1")).Data.Should().BeOfType<Reservation[]>().Which.Should().HaveCount(2);
    }
    [Fact]
    public void Save_failure_does_not_advance_session_state()
    {
        using var temp = new TempStore();
        var initial = Fixture.Ok(temp.Store.Load());
        var session = new CliSession(temp.Store, initial, () => Fixture.Noon, () => "new-id");
        var path = System.IO.Path.Combine(temp.Path, "audit.json");
        File.Delete(path); Directory.CreateDirectory(path);
        Fixture.Error(session.Execute("scan-card c1 r1")).Should().Be(ErrorCode.StorageError);
        session.State.Should().BeSameAs(initial);
    }
    [Fact]
    public void Repl_recovers_from_invalid_input_and_exits_on_eof()
    {
        using var temp = new TempStore();
        var session = new CliSession(temp.Store, Fixture.Ok(temp.Store.Load()), () => Fixture.Noon, () => "new-id");
        using var input = new StringReader("bad-command\nconfig show\n");
        using var output = new StringWriter();
        session.Run(input, output).Should().Be(0);
        output.ToString().Should().Contain("InvalidInput").And.Contain("normal");
    }
    [Fact]
    public void Help_works_without_a_store_and_startup_errors_have_nonzero_exit()
    {
        using var output = new StringWriter();
        CliApplication.Run(["--help"], new StringReader(""), output).Should().Be(0);
        output.ToString().Should().Contain("scan-card").And.Contain("--data");
        using var temp = new TempStore(false);
        CliApplication.Run(["--data", temp.Path, "config", "show"], new StringReader(""), output).Should().Be(1);
    }
}
