using Xunit;
using static BuildingSecurity.Tests.TestData;

namespace BuildingSecurity.Tests;

public sealed class FileTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "BuildingSecurityTests-" + Guid.NewGuid());

    internal const string ValidJson = """
        {
          "guests": [{"id":1,"name":"Ali","isBlocked":false}],
          "employees": [{"id":10,"name":"Host","maxGuests":0}],
          "rooms": [{"id":101,"name":"Lobby","buildingId":1}],
          "passes": [{"id":"P1","guestId":1,"employeeId":10,"buildingId":1,
            "validFrom":"2026-10-20T09:00:00Z","expiresAt":"2026-10-20T11:00:00Z","allowedRoomIds":[101]}],
          "visits": []
        }
        """;

    public FileTests() =>
        Directory.CreateDirectory(directory);

    internal string Write(string json)
    {
        var path = Path.Combine(
            directory,
            Guid.NewGuid() + ".json");

        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void ValidFile_PreservesFalseZeroAndReferences()
    {
        var state = Ok(Parser.Load(Write(ValidJson)));

        Assert.False(Assert.Single(state.Guests).IsBlocked);
        Assert.Equal(0, Assert.Single(state.Employees).MaxGuests);

        Assert.Equal(
            101,
            Assert.Single(
                Assert.Single(state.Passes).AllowedRoomIds));

        Assert.Empty(state.Visits);
    }

    [Fact]
    public void MissingFile_HasOperationAndPathContext()
    {
        var path = Path.Combine(directory, "missing.json");
        var error = Error(Parser.Load(path), "FileNotFound");

        Assert.Contains(path, error.Message);
        Assert.Contains("Load", error.Message);
    }

    [Theory]
    [InlineData("{ broken")]
    [InlineData("{}")]
    [InlineData("""{"guests":"wrong type"}""")]
    public void MalformedOrMissingFields_AreFailures(string json)
    {
        var path = Write(json);

        Assert.Contains(
            path,
            Error(Parser.Load(path), "MalformedFile").Message);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("""{"guests":null,"employees":[],"rooms":[],"passes":[],"visits":[]}""")]
    [InlineData("""{"guests":[null],"employees":[],"rooms":[],"passes":[],"visits":[]}""")]
    public void NullData_IsNotSuccessfulEmptyData(string json)
    {
        Error(Parser.Load(Write(json)), "InvalidData");
    }

    [Fact]
    public void MissingBoolean_DoesNotBecomeUnblockedGuest()
    {
        var json = ValidJson.Replace(",\"isBlocked\":false", "");

        Error(Parser.Load(Write(json)), "MalformedFile");
    }

    [Fact]
    public void MissingLimit_IsNotTheSameAsExplicitZero()
    {
        var json = ValidJson.Replace(",\"maxGuests\":0", "");

        Error(Parser.Load(Write(json)), "MalformedFile");

        Assert.Equal(
            0,
            Ok(Parser.Load(Write(ValidJson))).Employees[0].MaxGuests);
    }

    [Fact]
    public void ImportedVisits_PreserveExitOptionAndOnlyActiveVisitCounts()
    {
        var json = ValidJson.Replace("\"visits\": []", """
            "visits": [
              {"passId":"P1","roomId":101,"enteredAt":"2026-10-20T09:10:00Z","exitedAt":"2026-10-20T09:30:00Z"},
              {"passId":"P1","roomId":101,"enteredAt":"2026-10-20T10:00:00Z","exitedAt":null}
            ]
            """).Replace("\"maxGuests\":0", "\"maxGuests\":2");

        var state = Ok(Parser.Load(Write(json)));

        Assert.False(state.Visits[0].IsActive);

        Assert.Equal(
            Now.AddMinutes(-30),
            Assert.IsType<Option<DateTimeOffset>.SomeCase>(
                state.Visits[0].ExitedAt).Value);

        Assert.True(state.Visits[1].IsActive);

        Assert.IsType<Option<DateTimeOffset>.NoneCase>(
            state.Visits[1].ExitedAt);

        Assert.Equal(
            1,
            AccessPipeline.CountActiveGuests(state, 10));
    }

    [Fact]
    public void InvalidLaterRecord_RejectsTheWholeFile()
    {
        var json = ValidJson.Replace(
            "\"isBlocked\":false}",
            "\"isBlocked\":false},{\"id\":2,\"name\":\"\",\"isBlocked\":false}");

        Error(Parser.Load(Write(json)), "InvalidGuest");
    }

    [Fact]
    public void DuplicateAndDanglingReferences_AreRejectedWithFileContext()
    {
        var duplicate = Write(ValidJson.Replace(
            "\"isBlocked\":false}",
            "\"isBlocked\":false},{\"id\":1,\"name\":\"Other\",\"isBlocked\":false}"));

        Assert.Contains(
            duplicate,
            Error(Parser.Load(duplicate), "DuplicateId").Message);

        Error(
            Parser.Load(Write(
                ValidJson.Replace("\"employeeId\":10", "\"employeeId\":99"))),
            "InvalidReference");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidReload_DoesNotReplaceExistingStateOrRunSuccessCallback(
        bool missing)
    {
        var state = Data();
        var original = state;
        var calls = 0;

        var path = missing
            ? Path.Combine(directory, "missing.json")
            : Write("{ broken");

        var result = Parser.Reload(state, path).Map(next =>
        {
            calls++;
            state = next;
            return next;
        });

        Error(
            result,
            missing ? "FileNotFound" : "MalformedFile");

        Assert.Equal(0, calls);
        Assert.Same(original, state);
        Assert.Equal(3, state.Guests.Count);
    }

    [Fact]
    public void Reload_CannotEraseSessionVisits()
    {
        var state = Ok(AccessPipeline.ProcessEntry(
            Data(), new("P1", 101), Now)).NextState;

        Error(
            Parser.Reload(state, Write(ValidJson)),
            "ReloadWouldDiscardVisits");

        Assert.True(Assert.Single(state.Visits).IsActive);

        state = Ok(
            AccessPipeline.ProcessExit(state, "P1", Now)).NextState;

        Error(
            Parser.Reload(state, Write(ValidJson)),
            "ReloadWouldDiscardVisits");

        Assert.False(Assert.Single(state.Visits).IsActive);
    }

    [Fact]
    public void ReloadBeforeVisits_ReplacesWholeValidatedSnapshot()
    {
        var state = Ok(
            Parser.Reload(Data(), Write(ValidJson)));

        Assert.Single(state.Guests);
        Assert.Equal(0, state.Employees[0].MaxGuests);
    }

    public void Dispose() =>
        Directory.Delete(directory, recursive: true);
}