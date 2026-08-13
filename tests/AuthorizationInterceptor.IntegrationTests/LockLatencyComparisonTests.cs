using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.DistributedCache.Extensions;
using AuthorizationInterceptor.Extensions.MemoryCache.Extensions;
using AuthorizationInterceptor.IntegrationTests.Infrastructure;
using AuthorizationInterceptor.Options;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace AuthorizationInterceptor.IntegrationTests;

/// <summary>
/// Not a pass/fail benchmark: these tests keep the correctness assertions (login counts) and additionally print
/// a latency/throughput comparison across lock modes to the test output, so the cost of each lock is visible.
/// The lock modes trade a little response time for far fewer logins against the target API.
/// </summary>
[Collection(RedisCollection.Name)]
public class LockLatencyComparisonTests(RedisFixture redis, ITestOutputHelper output)
{
    private static readonly TimeSpan AuthDelay = TimeSpan.FromMilliseconds(200);

    [Fact]
    public async Task Latency_SingleInstance_NoneVsLocal()
    {
        const int requests = 50;
        output.WriteLine($"Single instance, {requests} concurrent requests, auth delay {AuthDelay.TotalMilliseconds:F0}ms");
        WriteHeader();

        var none = await RunSingleInstanceAsync(AuthenticationLockMode.None, requests);
        var local = await RunSingleInstanceAsync(AuthenticationLockMode.Local, requests);

        WriteRow("None", none.Logins, none.Load);
        WriteRow("Local", local.Logins, local.Load);

        Assert.All(none.Load.Responses.Concat(local.Load.Responses), response => Assert.True(response.IsSuccessStatusCode));
        Assert.True(none.Logins > 1);
        Assert.Equal(1, local.Logins);
    }

    [SkippableFact]
    public async Task Latency_AcrossInstances_NoneVsLocalVsDistributed()
    {
        Skip.IfNot(redis.IsAvailable, redis.SkipReason);

        const int instanceCount = 3;
        const int requestsPerInstance = 20;
        output.WriteLine($"{instanceCount} instances x {requestsPerInstance} concurrent requests, auth delay {AuthDelay.TotalMilliseconds:F0}ms");
        WriteHeader();

        var none = await RunAcrossInstancesAsync(AuthenticationLockMode.None, instanceCount, requestsPerInstance, shared: false);
        var local = await RunAcrossInstancesAsync(AuthenticationLockMode.Local, instanceCount, requestsPerInstance, shared: false);
        var distributedOnly = await RunAcrossInstancesAsync(AuthenticationLockMode.Distributed, instanceCount, requestsPerInstance, shared: true);
        var localAndDistributed = await RunAcrossInstancesAsync(AuthenticationLockMode.Local | AuthenticationLockMode.Distributed, instanceCount, requestsPerInstance, shared: true);

        WriteRow("None", none.Logins, none.Load);
        WriteRow("Local (isolated)", local.Logins, local.Load);
        WriteRow("Distributed", distributedOnly.Logins, distributedOnly.Load);
        WriteRow("Local|Distributed", localAndDistributed.Logins, localAndDistributed.Load);

        Assert.True(none.Logins > 1);
        Assert.Equal(instanceCount, local.Logins);
        Assert.Equal(1, distributedOnly.Logins);
        Assert.Equal(1, localAndDistributed.Logins);
    }

    private async Task<(int Logins, LoadResult Load)> RunSingleInstanceAsync(AuthenticationLockMode mode, int requests)
    {
        await using var target = await TargetApi.StartAsync(AuthDelay);
        await using var instance = ConsumerInstance.Create(target.CreateHandler, $"latency-{mode}", options =>
        {
            options.LockMode = mode;
            options.UseMemoryCacheInterceptor(services => services.AddMemoryCache());
        });

        var load = await Concurrency.MeasureConcurrentGetDataAsync(instance.Client, requests);
        return (target.Counter.Count, load);
    }

    private async Task<(int Logins, LoadResult Load)> RunAcrossInstancesAsync(AuthenticationLockMode mode, int instanceCount, int requestsPerInstance, bool shared)
    {
        await using var target = await TargetApi.StartAsync(AuthDelay);
        var key = $"latency-{mode}-{Guid.NewGuid():N}";

        var instances = Enumerable.Range(0, instanceCount)
            .Select(index => ConsumerInstance.Create(
                target.CreateHandler,
                shared ? key : $"{key}-{index}",
                options => ConfigureMode(options, mode, shared),
                shared ? ConfigureRedis : null))
            .ToArray();

        try
        {
            var load = await Concurrency.MeasureAcrossAsync(instances.Select(instance => instance.Client), requestsPerInstance);
            return (target.Counter.Count, load);
        }
        finally
        {
            foreach (var instance in instances)
                await instance.DisposeAsync();
        }
    }

    private static void ConfigureMode(AuthorizationInterceptorOptions options, AuthenticationLockMode mode, bool shared)
    {
        options.LockMode = mode;
        if (shared)
        {
            options.DistributedLockTimeout = TimeSpan.FromSeconds(30);
            options.UseDistributedCacheInterceptor();
        }
        else
        {
            options.UseMemoryCacheInterceptor(services => services.AddMemoryCache());
        }
    }

    private void ConfigureRedis(IServiceCollection services)
    {
        services.AddStackExchangeRedisCache(cache => cache.Configuration = redis.ConnectionString);
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis.ConnectionString));
        services.AddSingleton<IDistributedLockProvider>(provider =>
            new RedisDistributedSynchronizationProvider(provider.GetRequiredService<IConnectionMultiplexer>().GetDatabase()));
    }

    private void WriteHeader()
        => output.WriteLine($"{"Mode",-20} {"Logins",7} {"Total(ms)",10} {"Avg(ms)",9} {"P95(ms)",9} {"Max(ms)",9} {"req/s",8}");

    private void WriteRow(string mode, int logins, LoadResult load)
        => output.WriteLine($"{mode,-20} {logins,7} {load.Total.TotalMilliseconds,10:F0} {load.Average.TotalMilliseconds,9:F0} {load.P95.TotalMilliseconds,9:F0} {load.Max.TotalMilliseconds,9:F0} {load.RequestsPerSecond,8:F0}");
}
