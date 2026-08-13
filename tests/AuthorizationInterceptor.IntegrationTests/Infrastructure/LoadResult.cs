namespace AuthorizationInterceptor.IntegrationTests.Infrastructure;

/// <summary>
/// Timing outcome of a concurrent workload: the responses, the wall-clock time to drain them all, and the
/// per-request latencies used to compute the comparison statistics.
/// </summary>
public sealed record LoadResult(HttpResponseMessage[] Responses, TimeSpan Total, IReadOnlyList<TimeSpan> Latencies)
{
    public int Requests => Responses.Length;

    public TimeSpan Average => Latencies.Count == 0
        ? TimeSpan.Zero
        : TimeSpan.FromTicks((long)Latencies.Average(latency => latency.Ticks));

    public TimeSpan P95 => Percentile(95);

    public TimeSpan Max => Latencies.Count == 0 ? TimeSpan.Zero : Latencies.Max();

    public double RequestsPerSecond => Total.TotalSeconds <= 0 ? 0 : Requests / Total.TotalSeconds;

    private TimeSpan Percentile(int percentile)
    {
        if (Latencies.Count == 0)
            return TimeSpan.Zero;

        var ordered = Latencies.OrderBy(latency => latency).ToArray();
        var rank = (int)Math.Ceiling(percentile / 100d * ordered.Length) - 1;
        return ordered[Math.Clamp(rank, 0, ordered.Length - 1)];
    }
}
