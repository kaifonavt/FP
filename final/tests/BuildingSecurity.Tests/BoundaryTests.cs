using BuildingSecurity;
using FluentAssertions;
using Xunit;

namespace BuildingSecurity.Tests;

public class BoundaryTests
{
    [Fact]
    public void Inactive_card_skips_injected_access_policy()
    {
        var invoked = false;
        var outcome = Access.Scan(Fixture.State, "c4", "r1", Fixture.Noon, "a", (_, _, _) =>
        { invoked = true; return Result<bool>.Success(true); });
        invoked.Should().BeFalse();
        Fixture.Error(outcome.Decision).Should().Be(ErrorCode.InactiveAccountOrCard);
        outcome.State.Audit.Should().ContainSingle().Which.Granted.Should().BeFalse();
    }

    [Fact]
    public void Verified_card_rejects_another_employee_as_owner() =>
        Fixture.Error(VerifiedCard.Create(Fixture.State.Cards[0], Fixture.State.Employees[1]))
            .Should().Be(ErrorCode.InactiveAccountOrCard);

    [Fact]
    public void Configured_lockdown_threshold_changes_access()
    {
        var initial = Fixture.State;
        var restricted = initial with { Config = initial.Config with { Mode = BuildingMode.Lockdown } };
        var relaxed = restricted with { Config = restricted.Config with { LockdownMinLevel = 2 } };
        Access.Scan(restricted, "c2", "r1", Fixture.Noon, "a").State.Audit.Single().Granted.Should().BeFalse();
        Access.Scan(relaxed, "c2", "r1", Fixture.Noon, "b").State.Audit.Single().Granted.Should().BeTrue();
    }

    [Fact]
    public void Resolving_before_creation_is_rejected()
    {
        var created = Fixture.Ok(Tickets.Create(Fixture.State, "e1", "Lock", Fixture.Noon, "t"));
        Fixture.Error(Tickets.Resolve(created, "t", Fixture.Noon.AddSeconds(-1))).Should().Be(ErrorCode.InvalidInput);
        created.Tickets.Single().Status.Should().Be(TicketStatus.Open);
    }

    [Fact]
    public void Cancelling_twice_fails_and_preserves_original_booking()
    {
        var booked = Fixture.Ok(Reservations.Book(Fixture.State, "r1", "e1", Fixture.Noon, Fixture.Noon.AddHours(1), "b"));
        var cancelled = Fixture.Ok(Reservations.Cancel(booked, "b"));
        booked.Reservations.Single().Status.Should().Be(ReservationStatus.Active);
        Fixture.Error(Reservations.Cancel(cancelled, "b")).Should().Be(ErrorCode.AlreadyCancelled);
    }

    [Fact]
    public void Empty_data_produces_empty_reports()
    {
        Reports.AccessSummary(Fixture.State).Should().BeEmpty();
        Reports.RoomUtilization(Fixture.State).Should().BeEmpty();
        Reports.Anomalies([], 3).Should().BeEmpty();
    }

    [Fact]
    public void Mode_change_takes_effect_on_next_scan_in_same_session()
    {
        using var temp = new TempStore();
        var counter = 0;
        var session = new CliSession(temp.Store, Fixture.Ok(temp.Store.Load()), () => Fixture.Noon, () => "edge-" + ++counter);
        Fixture.Ok(session.Execute("scan-card c2 r1")).Failed.Should().BeFalse();
        Fixture.Ok(session.Execute("config set-mode night"));
        Fixture.Ok(session.Execute("scan-card c2 r1")).Failed.Should().BeTrue();
        Fixture.Ok(temp.Store.Load()).Audit.TakeLast(2).Select(a => a.Granted).Should().Equal(true, false);
    }

    [Fact]
    public void Multi_file_change_is_rejected_before_any_write()
    {
        using var temp = new TempStore();
        var before = Fixture.Ok(temp.Store.Load());
        var after = Access.Scan(before, "c1", "r1", Fixture.Noon, "extra-audit").State;
        after = after with { Config = after.Config with { Mode = BuildingMode.Lockdown } };
        Fixture.Error(temp.Store.SaveChange(before, after)).Should().Be(ErrorCode.InvalidInput);
        var reloaded = Fixture.Ok(temp.Store.Load());
        reloaded.Config.Should().Be(before.Config);
        reloaded.Audit.Should().Equal(before.Audit);
    }
}
