namespace BuildingSecurity;

public sealed class ScorePolicy
{
    public int ThresholdMinutes { get; }
    public int Weight { get; }
    private ScorePolicy(int threshold, int weight) => (ThresholdMinutes, Weight) = (threshold, weight);

    public static Result<ScorePolicy, AppError> Create(int thresholdMinutes, int weight) =>
        thresholdMinutes < 0 || weight is < 1 or > 100
            ? Result<ScorePolicy, AppError>.Failure(new("InvalidPolicy", "Threshold must be nonnegative; weight must be 1..100."))
            : Result<ScorePolicy, AppError>.Success(new(thresholdMinutes, weight));
}

public static class RiskScoring
{
    // Policy -> (duration -> score). The closure retains only an immutable policy.
    public static Func<long, int> CreateScore(ScorePolicy policy) => minutes =>
    {
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        // Clamp before multiplication, including for callers supplying long.MaxValue.
        var overdue = Math.Clamp(minutes - policy.ThresholdMinutes, 0, 100);
        return (int)Math.Min(100, overdue * policy.Weight);
    };
}
