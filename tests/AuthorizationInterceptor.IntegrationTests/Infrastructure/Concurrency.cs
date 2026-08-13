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
}
