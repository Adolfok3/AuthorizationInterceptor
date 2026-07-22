using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;

namespace AuthorizationInterceptor.Tests.Utils;

/// <summary>
/// Minimal in-memory caching interceptor, used to exercise the cache re-check that happens when a
/// caller had to wait on the authentication lock.
/// </summary>
/// <param name="readOnly">When true, writes are discarded, simulating a cache that never gets refreshed.</param>
public class MockCachingAuthorizationInterceptor(bool readOnly = false) : IAuthorizationInterceptor
{
    private readonly Dictionary<string, AuthorizationHeaders> _cache = [];

    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string key, CancellationToken cancellationToken)
    {
        lock (_cache)
        {
            _cache.TryGetValue(key, out var headers);
            return ValueTask.FromResult<AuthorizationHeaders?>(headers);
        }
    }

    public ValueTask UpdateHeadersAsync(string key, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
    {
        if (newHeaders == null || readOnly)
            return ValueTask.CompletedTask;

        lock (_cache)
            _cache[key] = newHeaders;

        return ValueTask.CompletedTask;
    }

    public void Seed(string key, AuthorizationHeaders headers)
    {
        lock (_cache)
            _cache[key] = headers;
    }
}
