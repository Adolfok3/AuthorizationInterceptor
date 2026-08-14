using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.DistributedCache.Extensions;
using AuthorizationInterceptor.IntegrationTests.Infrastructure;
using AuthorizationInterceptor.Options;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AuthorizationInterceptor.IntegrationTests;

[Collection(RedisCollection.Name)]
public class DistributedLockIntegrationTests(RedisFixture redis)
{
    private static readonly TimeSpan AuthDelay = TimeSpan.FromMilliseconds(200);

    [SkippableFact]
    public async Task LocalAndDistributedLock_ConcurrentRequestsAcrossInstances_ShouldAuthenticateOnce()
    {
        Skip.IfNot(redis.IsAvailable, redis.SkipReason);

        //Arrange
        await using var target = await TargetApi.StartAsync(AuthDelay);
        var sharedKey = $"local-distributed-{Guid.NewGuid():N}";

        //Act
        await RunAcrossInstancesAsync(target, sharedKey, AuthenticationLockMode.Local | AuthenticationLockMode.Distributed, instanceCount: 3, requestsPerInstance: 20);

        //Assert
        Assert.Equal(1, target.Counter.Count);
    }

    [SkippableFact]
    public async Task DistributedLockOnly_ConcurrentRequestsAcrossInstances_ShouldAuthenticateOnce()
    {
        Skip.IfNot(redis.IsAvailable, redis.SkipReason);

        //Arrange
        // Without the local single-flight, every request from every instance contends directly on the Redis lock.
        // The double-check after acquiring it still lets only the first caller authenticate; the rest adopt the
        // shared cache entry, so a single login happens across all threads and instances.
        await using var target = await TargetApi.StartAsync(AuthDelay);
        var sharedKey = $"distributed-only-{Guid.NewGuid():N}";

        //Act
        await RunAcrossInstancesAsync(target, sharedKey, AuthenticationLockMode.Distributed, instanceCount: 3, requestsPerInstance: 20);

        //Assert
        Assert.Equal(1, target.Counter.Count);
    }

    [SkippableFact]
    public async Task DistributedLockOnly_ConcurrentRequestsInSingleInstance_ShouldAuthenticateOnce()
    {
        Skip.IfNot(redis.IsAvailable, redis.SkipReason);

        //Arrange
        // Even inside one instance, with no local lock the distributed lock alone serializes the concurrent
        // threads and, together with the double-check, collapses them to a single login.
        await using var target = await TargetApi.StartAsync(AuthDelay);
        var sharedKey = $"distributed-only-single-{Guid.NewGuid():N}";
        await using var instance = CreateRedisInstance(target, sharedKey, AuthenticationLockMode.Distributed);

        //Act
        var responses = await Concurrency.FireConcurrentGetDataAsync(instance.Client, 50);

        //Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        Assert.Equal(1, target.Counter.Count);
    }

    private async Task RunAcrossInstancesAsync(TargetApi target, string sharedKey, AuthenticationLockMode mode, int instanceCount, int requestsPerInstance)
    {
        var instances = Enumerable.Range(0, instanceCount)
            .Select(_ => CreateRedisInstance(target, sharedKey, mode))
            .ToArray();

        try
        {
            var perInstance = await Task.WhenAll(instances.Select(instance => Concurrency.FireConcurrentGetDataAsync(instance.Client, requestsPerInstance)));
            var responses = perInstance.SelectMany(responses => responses).ToArray();

            Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        }
        finally
        {
            foreach (var instance in instances)
                await instance.DisposeAsync();
        }
    }

    private ConsumerInstance CreateRedisInstance(TargetApi target, string key, AuthenticationLockMode mode)
        => ConsumerInstance.Create(
            target.CreateHandler,
            key,
            options =>
            {
                options.LockMode = mode;
                options.DistributedLockTimeout = TimeSpan.FromSeconds(30);
                options.UseDistributedCacheInterceptor();
            },
            services =>
            {
                services.AddStackExchangeRedisCache(cache => cache.Configuration = redis.ConnectionString);
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis.ConnectionString));
                services.AddSingleton<IDistributedLockProvider>(provider =>
                    new RedisDistributedSynchronizationProvider(provider.GetRequiredService<IConnectionMultiplexer>().GetDatabase()));
            });
}
