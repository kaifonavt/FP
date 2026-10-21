using Xunit;
using static BuildingSecurity.Tests.TestData;

namespace BuildingSecurity.Tests;

public class AccessPipelineTests
{
    [Fact]
    public void CompleteSuccess_ProducesVisitAndMessageWithoutMutatingInput()
    {
        var state = Data();
        var result = Ok(
            AccessPipeline.ProcessEntry(state, new("P1", 101), Now));

        var visit = Assert.Single(result.NextState.Visits);

        Assert.Equal("P1", visit.PassId);
        Assert.Equal(101, visit.RoomId);
        Assert.Equal(Now, visit.EnteredAt);
        Assert.True(visit.IsActive);
        Assert.Contains("Ali", result.Message);
        Assert.Contains("101", result.Message);
        Assert.Empty(state.Visits);
    }

    [Fact]
    public void BlacklistedGuest_IsRejectedWithoutChangingState()
    {
        var state = Data(blocked: true);

        Assert.Contains(
            "1",
            Error(
                AccessPipeline.ProcessEntry(state, new("P1", 101), Now),
                "GuestBlocked").Message);

        Assert.Empty(state.Visits);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void ExpiryIsExclusive_AtOrAfterExpiryEntryIsRejected(int seconds)
    {
        var state = Data(expiresAt: Now.AddSeconds(seconds));

        Assert.Contains(
            "P1",
            Error(
                AccessPipeline.ProcessEntry(state, new("P1", 101), Now),
                "PassExpired").Message);

        Assert.Empty(state.Visits);
    }

    [Fact]
    public void BeforeExpiry_AndAtStart_EntryIsAllowed()
    {
        var state = Data(
            validFrom: Now,
            expiresAt: Now.AddSeconds(1));

        Assert.Single(
            Ok(AccessPipeline.ProcessEntry(
                state, new("P1", 101), Now)).NextState.Visits);
    }

    [Fact]
    public void PassNotYetValid_IsRejected()
    {
        Error(
            AccessPipeline.ProcessEntry(
                Data(validFrom: Now.AddMinutes(1)),
                new("P1", 101),
                Now),
            "PassNotStarted");
    }

    [Fact]
    public void UnauthorizedRoom_IsRejectedWithoutChangingState()
    {
        var state = Data();

        Assert.Contains(
            "102",
            Error(
                AccessPipeline.ProcessEntry(state, new("P1", 102), Now),
                "ZoneAccessDenied").Message);

        Assert.Empty(state.Visits);
    }

    [Fact]
    public void AnotherBuilding_IsRejected()
    {
        var state = Data();

        state = Ok(SecurityData.Create(
            state.Guests,
            state.Employees,
            state.Rooms.Append(
                Ok(Room.Create(201, "Other building", 2))),
            state.Passes,
            state.Visits));

        Error(
            AccessPipeline.ProcessEntry(state, new("P1", 201), Now),
            "ZoneAccessDenied");
    }

    [Fact]
    public void FullHostCapacity_IsRejectedAndExistingVisitPreserved()
    {
        var state = Data(maxGuests: 1);

        state = Ok(AccessPipeline.ProcessEntry(
            state, new("P2", 101), Now)).NextState;

        var previous = state.Visits[0];
        var result =
            AccessPipeline.ProcessEntry(state, new("P1", 101), Now);

        Assert.Contains(
            "10",
            Error(result, "HostGuestLimitExceeded").Message);

        Assert.Same(previous, Assert.Single(state.Visits));
        Assert.True(previous.IsActive);
    }

    [Fact]
    public void Exit_ReleasesCapacityAndKeepsHistory()
    {
        var state = Data(maxGuests: 1);

        state = Ok(AccessPipeline.ProcessEntry(
            state, new("P2", 101), Now)).NextState;

        var occupied = state;

        state = Ok(AccessPipeline.ProcessExit(
            state, "P2", Now.AddMinutes(1))).NextState;

        Assert.True(occupied.Visits[0].IsActive);
        Assert.False(state.Visits[0].IsActive);

        Assert.Equal(
            Now.AddMinutes(1),
            Assert.IsType<Option<DateTimeOffset>.SomeCase>(
                state.Visits[0].ExitedAt).Value);

        state = Ok(AccessPipeline.ProcessEntry(
            state, new("P1", 101), Now.AddMinutes(2))).NextState;

        Assert.Equal(2, state.Visits.Count);
        Assert.Single(state.Visits.Where(v => v.IsActive));
    }

    [Fact]
    public void ZeroCapacity_IsValidDataButRejectsEntry()
    {
        var state = Data(maxGuests: 0);

        Assert.Equal(0, state.Employees[0].MaxGuests);

        Error(
            AccessPipeline.ProcessEntry(state, new("P1", 101), Now),
            "HostGuestLimitExceeded");
    }

    [Fact]
    public void RepeatedEntry_DoesNotCreateAnotherVisit()
    {
        var state = Ok(AccessPipeline.ProcessEntry(
            Data(), new("P1", 101), Now)).NextState;

        Error(
            AccessPipeline.ProcessEntry(state, new("P1", 101), Now),
            "AlreadyInside");

        Assert.Single(state.Visits);
    }

    [Fact]
    public void SameGuestWithAnotherPass_CannotEnterTwice()
    {
        var state = Data();

        var second = Ok(TemporaryPass.Create(
            "P3", 1, 10, 1,
            Now.AddHours(-1),
            Now.AddHours(1),
            new[] { 101 }));

        state = Ok(SecurityData.Create(
            state.Guests,
            state.Employees,
            state.Rooms,
            state.Passes.Append(second),
            state.Visits));

        state = Ok(AccessPipeline.ProcessEntry(
            state, new("P1", 101), Now)).NextState;

        Error(
            AccessPipeline.ProcessEntry(state, new("P3", 101), Now),
            "AlreadyInside");

        Assert.Single(state.Visits);
    }

    [Fact]
    public void LookupPresentAbsentAndFalse_AreDistinct()
    {
        var state = Data();

        var guest = Assert.IsType<Option<Guest>.SomeCase>(
            AccessPipeline.Find(
                state.Guests, g => g.Id == 1)).Value;

        Assert.False(guest.IsBlocked);

        Assert.IsType<Option<Guest>.NoneCase>(
            AccessPipeline.Find(
                state.Guests, g => g.Id == 999));

        Assert.Contains(
            "P999",
            Error(
                AccessPipeline.ProcessEntry(
                    state, new("P999", 101), Now),
                "PassNotFound").Message);

        Error(
            AccessPipeline.ProcessEntry(state, new("P1", 999), Now),
            "RoomNotFound");
    }

    [Fact]
    public void DomainFailure_SkipsLaterPipelineOperation()
    {
        var request = Ok(AccessPipeline.EnrichRequest(
            Data(blocked: true), new("P1", 101), Now));

        var calls = 0;

        var result = AccessPipeline.CheckGuestBlocked(request)
            .Bind(value =>
            {
                calls++;
                return AccessPipeline.CheckPassValidity(value);
            });

        Error(result, "GuestBlocked");
        Assert.Equal(0, calls);
    }

    [Fact]
    public void RepeatedExit_AndExitBeforeEntry_AreRejected()
    {
        var state = Ok(AccessPipeline.ProcessEntry(
            Data(), new("P1", 101), Now)).NextState;

        Error(
            AccessPipeline.ProcessExit(
                state, "P1", Now.AddSeconds(-1)),
            "InvalidExitTime");

        Assert.True(state.Visits[0].IsActive);

        state = Ok(
            AccessPipeline.ProcessExit(state, "P1", Now)).NextState;

        Error(
            AccessPipeline.ProcessExit(state, "P1", Now),
            "NotInside");

        Error(
            AccessPipeline.ProcessExit(state, "unknown", Now),
            "PassNotFound");

        Assert.Single(state.Visits);
    }
}