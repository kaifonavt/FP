using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildingSecurity;

public static class Parser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static Result<SecurityData, AppError> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return FileFailure(
                "InvalidPath", path, "A file path is required.");
        }

        try
        {
            var dto = JsonSerializer.Deserialize<DataDto>(
                File.ReadAllText(path), Options);

            var result = dto is null
                ? Result<SecurityData, AppError>.Failure(
                    new("InvalidData",
                        "JSON root must be a data object."))
                : Build(dto);

            return result.Match(
                Result<SecurityData, AppError>.Success,
                error => FileFailure(error.Code, path, error.Message));
        }
        catch (FileNotFoundException error)
        {
            return FileFailure("FileNotFound", path, error.Message);
        }
        catch (DirectoryNotFoundException error)
        {
            return FileFailure("FileNotFound", path, error.Message);
        }
        catch (JsonException error)
        {
            return FileFailure("MalformedFile", path, error.Message);
        }
        catch (UnauthorizedAccessException error)
        {
            return FileFailure("FileReadError", path, error.Message);
        }
        catch (IOException error)
        {
            return FileFailure("FileReadError", path, error.Message);
        }
        catch (ArgumentException error)
        {
            return FileFailure("InvalidPath", path, error.Message);
        }
        catch (NotSupportedException error)
        {
            return FileFailure("InvalidPath", path, error.Message);
        }
    }

    public static Result<SecurityData, AppError> Reload(
        SecurityData current,
        string path) =>
        Load(path).Bind(next => current.Visits.Count == 0
            ? Result<SecurityData, AppError>.Success(next)
            : Result<SecurityData, AppError>.Failure(
                new("ReloadWouldDiscardVisits",
                    $"Reload '{path}' refused: visit history already exists. Restart explicitly to load a new session.")));

    private static Result<SecurityData, AppError> Build(DataDto dto) =>
        BuildRows(
                dto.Guests,
                x => Guest.Create(x.Id, x.Name, x.IsBlocked))
            .Bind(guests => BuildRows(
                    dto.Employees,
                    x => Employee.Create(x.Id, x.Name, x.MaxGuests))
            .Bind(employees => BuildRows(
                    dto.Rooms,
                    x => Room.Create(x.Id, x.Name, x.BuildingId))
            .Bind(rooms => BuildRows(
                    dto.Passes,
                    x => TemporaryPass.Create(
                        x.Id, x.GuestId, x.EmployeeId, x.BuildingId,
                        x.ValidFrom, x.ExpiresAt, x.AllowedRoomIds))
            .Bind(passes => BuildRows(
                    dto.Visits,
                    x => Visit.Create(
                        x.PassId, x.RoomId, x.EnteredAt,
                        x.ExitedAt.HasValue
                            ? Option<DateTimeOffset>.Some(x.ExitedAt.Value)
                            : Option<DateTimeOffset>.None))
            .Bind(visits => SecurityData.Create(
                guests, employees, rooms, passes, visits))))));

    private static Result<IReadOnlyList<T>, AppError> BuildRows<TDto, T>(
        TDto[]? rows,
        Func<TDto, Result<T, AppError>> create)
        where TDto : class
        where T : notnull
    {
        if (rows is null || rows.Any(x => x is null))
        {
            return Result<IReadOnlyList<T>, AppError>.Failure(
                new("InvalidData",
                    "Arrays and their records cannot be null."));
        }

        var values = new List<T>();

        foreach (var row in rows)
        {
            var result = create(row);

            if (result is Result<T, AppError>.FailureCase failure)
            {
                return Result<IReadOnlyList<T>, AppError>.Failure(
                    failure.Error);
            }

            values.Add(((Result<T, AppError>.SuccessCase)result).Value);
        }

        return Result<IReadOnlyList<T>, AppError>.Success(
            values.AsReadOnly());
    }

    private static Result<SecurityData, AppError> FileFailure(
        string code,
        string path,
        string message) =>
        Result<SecurityData, AppError>.Failure(
            new(code, $"Load '{path}': {message}"));

    private sealed class DataDto
    {
        public required GuestDto[]? Guests { get; init; }
        public required EmployeeDto[]? Employees { get; init; }
        public required RoomDto[]? Rooms { get; init; }
        public required PassDto[]? Passes { get; init; }
        public required VisitDto[]? Visits { get; init; }
    }

    private sealed class GuestDto
    {
        public required int Id { get; init; }
        public required string? Name { get; init; }
        public required bool IsBlocked { get; init; }
    }

    private sealed class EmployeeDto
    {
        public required int Id { get; init; }
        public required string? Name { get; init; }
        public required int MaxGuests { get; init; }
    }

    private sealed class RoomDto
    {
        public required int Id { get; init; }
        public required string? Name { get; init; }
        public required int BuildingId { get; init; }
    }

    private sealed class PassDto
    {
        public required string? Id { get; init; }
        public required int GuestId { get; init; }
        public required int EmployeeId { get; init; }
        public required int BuildingId { get; init; }
        public required DateTimeOffset ValidFrom { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
        public required int[]? AllowedRoomIds { get; init; }
    }

    private sealed class VisitDto
    {
        public required string? PassId { get; init; }
        public required int RoomId { get; init; }
        public required DateTimeOffset EnteredAt { get; init; }
        public required DateTimeOffset? ExitedAt { get; init; }
    }
}