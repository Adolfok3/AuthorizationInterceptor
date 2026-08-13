using Testcontainers.Redis;

namespace AuthorizationInterceptor.IntegrationTests.Infrastructure;

/// <summary>
/// Starts a real Redis instance in a container (via Testcontainers) shared by the distributed-lock tests.
/// When Docker is not available the container fails to start; <see cref="IsAvailable"/> is set to false and the
/// distributed tests skip instead of failing, so the suite still runs on machines without Docker.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsAvailable { get; private set; }

    public string SkipReason { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            IsAvailable = true;
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            SkipReason = $"Docker/Redis is not available: {exception.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _container.DisposeAsync();
        }
        catch
        {
            // Nothing to clean up when the container never started.
        }
    }
}

[CollectionDefinition(Name)]
public sealed class RedisCollection : ICollectionFixture<RedisFixture>
{
    public const string Name = "Redis";
}
