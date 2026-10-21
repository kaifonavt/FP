using Xunit;
using static BuildingSecurity.Tests.TestData;

namespace BuildingSecurity.Tests;

public class ModelTests
{
    [Fact]
    public void Factories_RejectInvalidFieldsWithoutThrowingDomainExceptions()
    {
        Error(Guest.Create(0, "Ali", false), "InvalidGuest");
        Error(Guest.Create(1, " ", false), "InvalidGuest");
        Error(Employee.Create(10, "Host", -1), "InvalidEmployee");
        Error(Room.Create(1, "Room", 0), "InvalidRoom");

        Error(
            TemporaryPass.Create(
                "P", 1, 10, 1, Now, Now, new[] { 101 }),
            "InvalidPass");

        Error(
            TemporaryPass.Create(
                "P", 1, 10, 1, Now, Now.AddHours(1),
                Array.Empty<int>()),
            "InvalidPass");

        Error(
            Visit.Create(
                "P", 101, Now,
                Option<DateTimeOffset>.Some(Now.AddMinutes(-1))),
            "InvalidVisit");
    }

    [Fact]
    public void FactoryTrimsNames_AndCopiesAllowedRooms()
    {
        Assert.Equal("Ali", Ok(Guest.Create(1, " Ali ", false)).Name);

        var rooms = new[] { 101 };

        var pass = Ok(TemporaryPass.Create(
            " P1 ", 1, 10, 1, Now, Now.AddHours(1), rooms));

        rooms[0] = 999;

        Assert.Equal("P1", pass.Id);
        Assert.Equal(101, Assert.Single(pass.AllowedRoomIds));
    }

    [Fact]
    public void SnapshotRejectsDuplicateIdsAndDanglingReferences()
    {
        var state = Data();

        Error(
            SecurityData.Create(
                state.Guests.Append(state.Guests[0]),
                state.Employees,
                state.Rooms,
                state.Passes,
                state.Visits),
            "DuplicateId");

        Error(
            SecurityData.Create(
                state.Guests,
                Array.Empty<Employee>(),
                state.Rooms,
                state.Passes,
                state.Visits),
            "InvalidReference");
    }

    [Fact]
    public void SnapshotCopiesCollections_AndAllowsAnExplicitEmptySnapshot()
    {
        var guests = new List<Guest>
        {
            Ok(Guest.Create(1, "Ali", false))
        };

        var state = Ok(SecurityData.Create(
            guests,
            Array.Empty<Employee>(),
            Array.Empty<Room>(),
            Array.Empty<TemporaryPass>(),
            Array.Empty<Visit>()));

        guests.Clear();
        Assert.Single(state.Guests);

        var empty = Ok(SecurityData.Create(
            Array.Empty<Guest>(),
            Array.Empty<Employee>(),
            Array.Empty<Room>(),
            Array.Empty<TemporaryPass>(),
            Array.Empty<Visit>()));

        Assert.Empty(empty.Guests);

        Error(
            SecurityData.Create(
                null, empty.Employees, empty.Rooms,
                empty.Passes, empty.Visits),
            "InvalidData");
    }

    [Fact]
    public void SnapshotRejectsDuplicateActiveVisitAndUnauthorizedHistory()
    {
        var state = Data();

        var active = Ok(Visit.Create(
            "P1", 101, Now, Option<DateTimeOffset>.None));

        Error(
            SecurityData.Create(
                state.Guests, state.Employees, state.Rooms,
                state.Passes, new[] { active, active }),
            "DuplicateActiveVisit");

        var wrongRoom = Ok(Visit.Create(
            "P1", 102, Now, Option<DateTimeOffset>.None));

        Error(
            SecurityData.Create(
                state.Guests, state.Employees, state.Rooms,
                state.Passes, new[] { wrongRoom }),
            "InvalidVisit");
    }
}