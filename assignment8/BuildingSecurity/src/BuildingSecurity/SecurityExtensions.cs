namespace BuildingSecurity;

public sealed record TimedVisit(Visit Visit, long Minutes);

public static class SecurityExtensions
{
    // Both extensions are lazy and leave the source collection unchanged.
    public static IEnumerable<Visit> ActiveOnly(this IEnumerable<Visit> visits) =>
        visits.Where(visit => visit.IsActive);

    // The report boundary validates timestamps before enumerating this pure iterator.
    public static IEnumerable<TimedVisit> WithDuration(
        this IEnumerable<Visit> visits, DateTimeOffset asOf)
    {
        foreach (var visit in visits)
        {
            var end = visit.ExitedAt.Match(exit => exit, () => asOf);
            if (end < visit.EnteredAt)
                throw new ArgumentOutOfRangeException(nameof(asOf), "Report time precedes entry.");
            yield return new TimedVisit(visit, (end - visit.EnteredAt).Ticks / TimeSpan.TicksPerMinute);
        }
    }
}
