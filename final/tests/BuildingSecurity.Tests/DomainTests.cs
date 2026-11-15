using System.Collections.Immutable;
using BuildingSecurity;
using FluentAssertions;
using Xunit;

namespace BuildingSecurity.Tests;

public static class Fixture
{
    public static readonly DateTimeOffset Noon = new(2026, 11, 12, 12, 0, 0, TimeSpan.FromHours(5));
    public static SecurityState State => new(
        [new("e1", "Anna", "IT", 5, true), new("e2", "Boris", "Sales", 2, true), new("e3", "Inactive", "IT", 5, false)],
        [new("c1", "e1", true), new("c2", "e2", true), new("c3", "e3", true), new("c4", "e1", false)],
        [new("r1", "Meeting", "meeting", 1), new("r2", "Server", "restricted", 5)],
        [], [], [], new(BuildingMode.Normal, TimeSpan.FromHours(8), TimeSpan.FromHours(18), 5));
    public static T Ok<T>(Result<T> result) => result.Should().BeOfType<Result<T>.Ok>().Subject.Value;
    public static ErrorCode Error<T>(Result<T> result) => result.Should().BeOfType<Result<T>.Fail>().Subject.Error.Code;
}

public class DomainTests
{
    [Fact] public void Map_transforms_success() => Fixture.Ok(Result<int>.Success(3).Map(x => x * 2)).Should().Be(6);
    [Fact]
    public void Bind_short_circuits_failure()
    {
        var called = false;
        var result = Result<int>.Failure(ErrorCode.EntityNotFound, "missing")
            .Bind(x => { called = true; return Result<int>.Success(x); }).Map(x => x + 1);
        called.Should().BeFalse(); Fixture.Error(result).Should().Be(ErrorCode.EntityNotFound);
    }
    [Fact]
    public void Maybe_lookup_explicitly_represents_absence()
    {
        Fixture.State.Cards.FindMaybe(c => c.Id == "missing").Should().BeOfType<Maybe<Card>.None>();
        Fixture.Error(Fixture.State.Cards.FindMaybe(c => c.Id == "missing")
            .ToResult(new(ErrorCode.EntityNotFound, "card"))).Should().Be(ErrorCode.EntityNotFound);
    }
    [Theory]
    [InlineData("c1", "r2", true)]
    [InlineData("c2", "r2", false)]
    [InlineData("c3", "r1", false)]
    [InlineData("c4", "r1", false)]
    [InlineData("unknown", "r1", false)]
    [InlineData("c1", "unknown", false)]
    public void Scan_audits_every_outcome_without_mutating_input(string card, string room, bool granted)
    {
        var before = Fixture.State;
        var outcome = Access.Scan(before, card, room, Fixture.Noon, "a1");
        outcome.State.Audit.Should().ContainSingle().Which.Granted.Should().Be(granted);
        before.Audit.Should().BeEmpty();
        (outcome.Decision is Result<AccessDecision>.Ok).Should().Be(granted);
    }
    [Theory]
    [InlineData(BuildingMode.Normal, "c2", 8, true)]
    [InlineData(BuildingMode.Normal, "c2", 18, false)]
    [InlineData(BuildingMode.Night, "c2", 12, false)]
    [InlineData(BuildingMode.Night, "c1", 23, true)]
    [InlineData(BuildingMode.Lockdown, "c2", 12, false)]
    [InlineData(BuildingMode.Lockdown, "c1", 12, true)]
    public void Access_modes_and_hours_change_business_rules(BuildingMode mode, string card, int hour, bool allowed)
    {
        var s = Fixture.State; s = s with { Config = s.Config with { Mode = mode } };
        Access.Scan(s, card, "r1", Fixture.Noon.AddHours(hour - 12), "a").State.Audit.Single().Granted.Should().Be(allowed);
    }
    [Fact]
    public void Overnight_hours_are_half_open()
    {
        var c = Fixture.State.Config with { OpenTime = TimeSpan.FromHours(22), CloseTime = TimeSpan.FromHours(6) };
        Access.IsWithinHours(c, TimeSpan.Zero).Should().BeTrue();
        Access.IsWithinHours(c, TimeSpan.FromHours(6)).Should().BeFalse();
    }
    [Fact]
    public void Injected_policy_changes_access()
    {
        var result = Access.Scan(Fixture.State, "c1", "r1", Fixture.Noon, "a", (_, _, _) =>
            Result<bool>.Failure(ErrorCode.AccessDenied, "maintenance"));
        Fixture.Error(result.Decision).Should().Be(ErrorCode.AccessDenied);
    }
    [Fact]
    public void Reservations_reject_overlap_but_allow_adjacent_intervals()
    {
        var t = Fixture.Noon;
        var booked = Fixture.Ok(Reservations.Book(Fixture.State, "r1", "e1", t, t.AddHours(1), "b1"));
        Fixture.Error(Reservations.Book(booked, "r1", "e2", t.AddMinutes(59), t.AddHours(2), "b2")).Should().Be(ErrorCode.TimeSlotConflict);
        Fixture.Ok(Reservations.Book(booked, "r1", "e2", t.AddHours(1), t.AddHours(2), "b2")).Reservations.Should().HaveCount(2);
    }
    [Fact]
    public void Offset_equivalent_bookings_conflict()
    {
        var t = Fixture.Noon;
        var s = Fixture.Ok(Reservations.Book(Fixture.State, "r1", "e1", t, t.AddHours(1), "b1"));
        Fixture.Error(Reservations.Book(s, "r1", "e1", t.ToUniversalTime(), t.AddHours(1).ToUniversalTime(), "b2"))
            .Should().Be(ErrorCode.TimeSlotConflict);
    }
    [Fact]
    public void Cancellation_releases_slot()
    {
        var t = Fixture.Noon;
        var s = Fixture.Ok(Reservations.Book(Fixture.State, "r1", "e1", t, t.AddHours(1), "b1"));
        s = Fixture.Ok(Reservations.Cancel(s, "b1"));
        Fixture.Ok(Reservations.Book(s, "r1", "e2", t, t.AddHours(1), "b2")).Reservations.Should().HaveCount(2);
    }
    [Fact]
    public void Invalid_interval_and_inactive_employee_are_rejected()
    {
        var t = Fixture.Noon;
        Fixture.Error(Reservations.Book(Fixture.State, "r1", "e1", t, t, "b")).Should().Be(ErrorCode.InvalidInput);
        Fixture.Error(Reservations.Book(Fixture.State, "r1", "e3", t, t.AddHours(1), "b")).Should().Be(ErrorCode.InactiveAccountOrCard);
        Fixture.Error(Reservations.Book(Fixture.State, "r2", "e2", t, t.AddHours(1), "b")).Should().Be(ErrorCode.InsufficientClearance);
    }
    [Fact]
    public void Ticket_cannot_be_resolved_twice()
    {
        var s = Fixture.Ok(Tickets.Create(Fixture.State, "e1", "Broken lock", Fixture.Noon, "t1"));
        s = Fixture.Ok(Tickets.Resolve(s, "t1", Fixture.Noon.AddMinutes(1)));
        s.Tickets.Single().Status.Should().Be(TicketStatus.Closed);
        Fixture.Error(Tickets.Resolve(s, "t1", Fixture.Noon.AddMinutes(2))).Should().Be(ErrorCode.TicketAlreadyResolved);
        Fixture.Error(Tickets.Create(s, "missing", "Broken", Fixture.Noon, "t2")).Should().Be(ErrorCode.EntityNotFound);
    }
    [Fact]
    public void Audit_stream_is_lazy_and_materialization_is_reusable()
    {
        var count = 0;
        IEnumerable<AuditEntry> Source()
        {
            count++; yield return new("a1", Fixture.Noon, "c1", "e1", "r1", false, "denied");
            count++; yield return new("a2", Fixture.Noon, "c1", "e1", "r1", true, "ok");
        }
        var lazy = Source().StreamWhere(Reports.AuditFilter("e1")("r1")(true));
        count.Should().Be(0);
        var snapshot = lazy.ToList(); snapshot.Should().ContainSingle(); count.Should().Be(2);
        snapshot.Count.Should().Be(1); count.Should().Be(2);
    }
    [Fact]
    public void Reports_aggregate_and_break_ties_deterministically()
    {
        var s = Access.Scan(Fixture.State, "c2", "r2", Fixture.Noon, "a1").State;
        s = Access.Scan(s, "c1", "missing", Fixture.Noon, "a2").State;
        var report = Reports.AccessSummary(s);
        report.Select(x => x.EmployeeName).Should().Equal("Anna", "Boris");
        report.Select(x => x.Denied).Should().Equal(1, 1);
        s = Fixture.Ok(Reservations.Book(s, "r1", "e1", Fixture.Noon, Fixture.Noon.AddHours(2), "b1"));
        s = Fixture.Ok(Reservations.Book(s, "r2", "e1", Fixture.Noon, Fixture.Noon.AddHours(2), "b2"));
        Reports.RoomUtilization(s).Select(x => x.RoomId).Should().Equal("r1", "r2");
        Reports.RoomUtilization(s)[0].AverageHours.Should().Be(2);
        Reports.AccessSummary(Fixture.State).Should().BeEmpty();
    }
}
