using System.Diagnostics;

namespace AuthorizationInterceptor.IntegrationTests.Infrastructure;

public static class Concurrency
{
    /// <summary>
    /// Fires <paramref name="count"/> GET /data requests that are all released simultaneously through a gate,
    /// so they overlap as much as possible and genuinely race for the (missing) authorization headers.
    /// </summary>
    public static async Task<HttpResponseMessage[]> FireConcurrentGetDataAsync(HttpClient client, int count)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var requests = Enumerable.Range(0, count)
            .Select(_ => Task.Run(async () =>
            {
                await gate.Task;
                return await client.GetAsync("/data");
            }))
            .ToArray();

        gate.SetResult();

        return await Task.WhenAll(requests);
    }

    /// <summary>
    /// Same simultaneous burst as <see cref="FireConcurrentGetDataAsync"/>, but also records the per-request
    /// latency and the total wall-clock time so different lock modes can be compared.
    /// </summary>
    public static async Task<LoadResult> MeasureConcurrentGetDataAsync(HttpClient client, int count)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var latencies = new TimeSpan[count];

        var requests = Enumerable.Range(0, count)
            .Select(index => Task.Run(async () =>
            {
                await gate.Task;
                var started = Stopwatch.GetTimestamp();
                var response = await client.GetAsync("/data");
                latencies[index] = Stopwatch.GetElapsedTime(started);
                return response;
            }))
            .ToArray();

        var total = Stopwatch.StartNew();
        gate.SetResult();
        var responses = await Task.WhenAll(requests);
        total.Stop();

        return new LoadResult(responses, total.Elapsed, latencies);
    }

    /// <summary>
    /// Runs <paramref name="requestsPerClient"/> concurrent requests on every client at once (modeling several
    /// instances under simultaneous load) and aggregates the latencies and total wall-clock time.
    /// </summary>
    public static async Task<LoadResult> MeasureAcrossAsync(IEnumerable<HttpClient> clients, int requestsPerClient)
    {
        var total = Stopwatch.StartNew();
        var perClient = await Task.WhenAll(clients.Select(client => MeasureConcurrentGetDataAsync(client, requestsPerClient)));
        total.Stop();

        var responses = perClient.SelectMany(result => result.Responses).ToArray();
        var latencies = perClient.SelectMany(result => result.Latencies).ToList();

        return new LoadResult(responses, total.Elapsed, latencies);
    }
}
