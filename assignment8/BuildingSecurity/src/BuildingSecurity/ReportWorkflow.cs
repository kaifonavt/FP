namespace BuildingSecurity;

public static class ReportWorkflow
{
    public static Result<ReportBundle, AppError> Build(SecurityData state, DateTimeOffset asOf,
        NativeLoadAdapter native, long minimumMinutes = 0)
    {
        return ScorePolicy.Create(60, 1).Bind(dayPolicy =>
            ScorePolicy.Create(30, 2).Bind(nightPolicy =>
                BuildConfigured(state, asOf, native, RiskScoring.CreateScore(dayPolicy),
                    RiskScoring.CreateScore(nightPolicy), minimumMinutes)));
    }

    public static Result<ReportBundle, AppError> BuildConfigured(SecurityData state,
        DateTimeOffset asOf, NativeLoadAdapter native, Func<long, int> day,
        Func<long, int> night, long minimumMinutes = 0)
    {
        return SecurityReports.Prepare(state, asOf).Bind(rows =>
            SecurityReports.Attention(rows, minimumMinutes, day, night).Bind(attention =>
            {
                var loads = new List<HostLoadRow>();
                foreach (var host in SecurityReports.HostTotals(rows))
                {
                    var result = native.Calculate(host.ActiveGuests, host.Capacity);
                    if (result is Result<int, AppError>.FailureCase failure)
                        return Result<ReportBundle, AppError>.Failure(failure.Error);
                    var percent = ((Result<int, AppError>.SuccessCase)result).Value;
                    loads.Add(new HostLoadRow(host.HostId, host.HostName, host.Capacity,
                        host.ActiveGuests, host.TotalMinutes, percent));
                }

                return Result<ReportBundle, AppError>.Success(new ReportBundle(asOf, attention, loads.AsReadOnly()));
            }));
    }

    public static Result<string, AppError> Export(SecurityData state, DateTimeOffset asOf,
        NativeLoadAdapter native, string path)
    {
        return Build(state, asOf, native).Bind(report => ReportExporter.Save(report, path));
    }
}