using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.MemoryCache.Extensions;
using AuthorizationInterceptor.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.IntegrationTests;

public class LocalLockIntegrationTests
{
    private static readonly TimeSpan AuthDelay = TimeSpan.FromMilliseconds(200);

    [Fact]
    public async Task LocalLock_ConcurrentRequests_ShouldAuthenticateOnce()
    {
        //Arrange
        await using var target = await TargetApi.StartAsync(AuthDelay);
        await using var instance = ConsumerInstance.Create(target.CreateHandler, "local-single-flight", options =>
        {
            options.LockMode = AuthenticationLockMode.Local;
            options.UseMemoryCacheInterceptor(services => services.AddMemoryCache());
        });

        //Act
        var responses = await Concurrency.FireConcurrentGetDataAsync(instance.Client, 50);

        //Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        Assert.Equal(1, target.Counter.Count);
    }

    [Fact]
    public async Task NoLock_ConcurrentRequests_ShouldAuthenticateMoreThanOnce()
    {
        //Arrange
        // Control: without any lock, concurrent callers that all miss the cold cache each authenticate.
        // The assertion is tolerant (> 1) because the exact number depends on timing.
        await using var target = await TargetApi.StartAsync(AuthDelay);
        await using var instance = ConsumerInstance.Create(target.CreateHandler, "local-none", options =>
        {
            options.LockMode = AuthenticationLockMode.None;
            options.UseMemoryCacheInterceptor(services => services.AddMemoryCache());
        });

        //Act
        var responses = await Concurrency.FireConcurrentGetDataAsync(instance.Client, 50);

        //Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        Assert.True(target.Counter.Count > 1, $"expected more than one login without a lock, got {target.Counter.Count}");
    }

    [Fact]
    public async Task LocalLockOnly_AcrossInstances_ShouldAuthenticateOncePerInstance()
    {
        //Arrange
        // Control for the distributed scenario, but Docker-free: each instance keeps its own memory cache and
        // only a local lock, so nothing is shared across instances and each one authenticates exactly once.
        await using var target = await TargetApi.StartAsync(AuthDelay);
        const int instanceCount = 3;

        var instances = Enumerable.Range(0, instanceCount)
            .Select(index => ConsumerInstance.Create(target.CreateHandler, $"local-isolated-{index}", options =>
            {
                options.LockMode = AuthenticationLockMode.Local;
                options.UseMemoryCacheInterceptor(services => services.AddMemoryCache());
            }))
            .ToArray();

        try
        {
            //Act
            var perInstance = await Task.WhenAll(instances.Select(instance => Concurrency.FireConcurrentGetDataAsync(instance.Client, 20)));
            var responses = perInstance.SelectMany(responses => responses).ToArray();

            //Assert
            Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
            Assert.Equal(instanceCount, target.Counter.Count);
        }
        finally
        {
            foreach (var instance in instances)
                await instance.DisposeAsync();
        }
    }
}
