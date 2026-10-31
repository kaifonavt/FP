using Xunit;

namespace BuildingSecurity.Tests;

public class FunctionalTests
{
    [Fact]
    public void Map_TransformsTheSuccessfulPayload()
    {
        var result = Result<int, string>.Success(3).Map(x => x * 4);

        Assert.Equal(
            12,
            Assert.IsType<Result<int, string>.SuccessCase>(result).Value);
    }

    [Fact]
    public void Bind_PassesPayloadAndPreservesReturnedResult()
    {
        var expected = Result<string, string>.Success("six");

        var actual = Result<int, string>.Success(6).Bind(value =>
        {
            Assert.Equal(6, value);
            return expected;
        });

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Bind_CanTurnSuccessIntoFailure()
    {
        var result = Result<int, string>.Success(0)
            .Bind(_ => Result<string, string>.Failure("No capacity"));

        Assert.Equal(
            "No capacity",
            Assert.IsType<Result<string, string>.FailureCase>(result).Error);
    }

    [Fact]
    public void Map_PropagatesSameErrorWithoutCallingCallback()
    {
        var error = new AppError("Blocked", "Guest 2 is blocked");
        var calls = 0;

        var result = Result<int, AppError>.Failure(error).Map(value =>
        {
            calls++;
            return value + 1;
        });

        Assert.Same(
            error,
            Assert.IsType<Result<int, AppError>.FailureCase>(result).Error);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void Bind_PropagatesSameErrorWithoutCallingCallback()
    {
        var error = new AppError("Missing", "Pass P9 not found");
        var calls = 0;

        var result = Result<int, AppError>.Failure(error).Bind(value =>
        {
            calls++;
            return Result<string, AppError>.Success(value.ToString());
        });

        Assert.Same(
            error,
            Assert.IsType<Result<string, AppError>.FailureCase>(result).Error);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void Match_HandlesOnlyTheActualCase()
    {
        Assert.Equal(
            "value=5",
            Result<int, string>.Success(5)
                .Match(x => $"value={x}", _ => "wrong"));

        Assert.Equal(
            "error=denied",
            Result<int, string>.Failure("denied")
                .Match(_ => "wrong", e => $"error={e}"));
    }

    [Fact]
    public void SomeFalseAndZero_ArePresentValues()
    {
        Assert.False(
            Assert.IsType<Option<bool>.SomeCase>(
                Option<bool>.Some(false)).Value);

        var zero = Option<int>.Some(0).ToResult("missing");

        Assert.Equal(
            0,
            Assert.IsType<Result<int, string>.SuccessCase>(zero).Value);

        Assert.Equal(
            "found",
            Option<bool>.Some(false)
                .Match(_ => "found", () => "missing"));
    }

    [Fact]
    public void None_ConvertsToMeaningfulFailure()
    {
        var error = new AppError("GuestNotFound", "Guest 999 not found");

        var result = Option<int>.None.ToResult(error);

        Assert.Same(
            error,
            Assert.IsType<Result<int, AppError>.FailureCase>(result).Error);

        Assert.Equal(
            "absent",
            Option<int>.None.Match(_ => "wrong", () => "absent"));
    }

    [Fact]
    public void NullPayloads_AreRejectedInsteadOfCreatingContradictoryCases()
    {
        Assert.Throws<ArgumentNullException>(
            () => Result<string, string>.Success(default!));

        Assert.Throws<ArgumentNullException>(
            () => Result<string, string>.Failure(default!));

        Assert.Throws<ArgumentNullException>(
            () => Option<string>.Some(default!));

        Assert.Throws<ArgumentNullException>(
            () => Result<string, string>.Success("ok")
                .Map<string>(_ => default!));
    }
}