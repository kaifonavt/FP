using Xunit;

namespace BuildingSecurity.Tests;

internal static class TestData
{
    internal static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-10-20T10:00:00Z");

    internal static T Ok<T>(Result<T, AppError> result)
        where T : notnull =>
        Assert.IsType<Result<T, AppError>.SuccessCase>(result).Value;

    internal static AppError Error<T>(
        Result<T, AppError> result,
        string code) where T : notnull
    {
        var error =
            Assert.IsType<Result<T, AppError>.FailureCase>(result).Error;

        Assert.Equal(code, error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        return error;
    }

    internal static SecurityData Data(
        bool blocked = false,
        int maxGuests = 2,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? validFrom = null)
    {
        var guests = new[]
        {
            Ok(Guest.Create(1, "Ali", blocked)),
            Ok(Guest.Create(2, "Dana", false)),
            Ok(Guest.Create(3, "Sam", false))
        };

        var host = Ok(Employee.Create(10, "Batyrkhan", maxGuests));

        var rooms = new[]
        {
            Ok(Room.Create(101, "Meeting room", 1)),
            Ok(Room.Create(102, "Server room", 1))
        };

        var pass = Ok(TemporaryPass.Create(
            "P1", 1, 10, 1,
            validFrom ?? Now.AddHours(-1),
            expiresAt ?? Now.AddHours(1),
            new[] { 101 }));

        var other = Ok(TemporaryPass.Create(
            "P2", 2, 10, 1,
            Now.AddHours(-1),
            Now.AddHours(1),
            new[] { 101 }));

        return Ok(SecurityData.Create(
            guests,
            new[] { host },
            rooms,
            new[] { pass, other },
            Array.Empty<Visit>()));
    }
}