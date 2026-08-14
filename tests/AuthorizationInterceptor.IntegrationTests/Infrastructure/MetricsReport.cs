using System.Text;

namespace AuthorizationInterceptor.IntegrationTests.Infrastructure;

/// <summary>
/// Writes a lock-latency comparison as a Markdown table to the directory named by the <c>LOCK_METRICS_DIR</c>
/// environment variable, when set. It is intentionally CI-agnostic: the test only produces an artifact file, and
/// the workflow decides what to do with it (e.g. append it to the GitHub job summary). When the variable is not
/// set (local runs) it is a no-op.
/// </summary>
public static class MetricsReport
{
    public static void Write(string fileName, string title, IReadOnlyList<(string Mode, int Logins, LoadResult Load)> rows)
    {
        var directory = Environment.GetEnvironmentVariable("LOCK_METRICS_DIR");
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);

        var builder = new StringBuilder();
        builder.AppendLine($"### {title}").AppendLine();
        builder.AppendLine("| Mode | Logins | Total (ms) | Avg (ms) | P95 (ms) | Max (ms) | req/s |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: |");

        foreach (var (mode, logins, load) in rows)
            builder.AppendLine($"| {mode.Replace("|", "\\|")} | {logins} | {load.Total.TotalMilliseconds:F0} | {load.Average.TotalMilliseconds:F0} | {load.P95.TotalMilliseconds:F0} | {load.Max.TotalMilliseconds:F0} | {load.RequestsPerSecond:F0} |");

        builder.AppendLine();

        File.WriteAllText(Path.Combine(directory, fileName), builder.ToString());
    }
}
