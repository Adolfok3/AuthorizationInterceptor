using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.DistributedCache.Extensions;
using AuthorizationInterceptor.IntegrationTests.Infrastructure;
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
    public async Task DistributedLock_ConcurrentRequestsAcrossInstances_ShouldAuthenticateOnce()
    {
        Skip.IfNot(redis.IsAvailable, redis.SkipReason);

        //Arrange
        await using var target = await TargetApi.StartAsync(AuthDelay);
        const int instanceCount = 3;
        const int requestsPerInstance = 20;
        // A per-run key namespaces this test's lock/cache entries in the shared Redis. All instances share it,
        // so they contend on the same distributed lock and read/write the same distributed cache entry.
        var sharedKey = $"dist-single-login-{Guid.NewGuid():N}";

        var instances = Enumerable.Range(0, instanceCount)
            .Select(_ => ConsumerInstance.Create(target.CreateHandler, sharedKey,
                options =>
                {
                    options.LockMode = AuthenticationLockMode.Local | AuthenticationLockMode.Distributed;
                    options.DistributedLockTimeout = TimeSpan.FromSeconds(30);
                    options.UseDistributedCacheInterceptor();
                },
                services =>
                {
                    services.AddStackExchangeRedisCache(cache => cache.Configuration = redis.ConnectionString);
                    services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis.ConnectionString));
                    services.AddSingleton<IDistributedLockProvider>(provider =>
                        new RedisDistributedSynchronizationProvider(provider.GetRequiredService<IConnectionMultiplexer>().GetDatabase()));
                }))
            .ToArray();

        try
        {
            //Act
            var perInstance = await Task.WhenAll(instances.Select(instance => Concurrency.FireConcurrentGetDataAsync(instance.Client, requestsPerInstance)));
            var responses = perInstance.SelectMany(responses => responses).ToArray();

            //Assert
            Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
            Assert.Equal(1, target.Counter.Count);
        }
        finally
        {
            foreach (var instance in instances)
                await instance.DisposeAsync();
        }
    }
}
