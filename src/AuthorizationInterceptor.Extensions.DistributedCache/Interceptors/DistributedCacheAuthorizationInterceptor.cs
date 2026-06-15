using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.Abstractions.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace AuthorizationInterceptor.Extensions.DistributedCache.Interceptors;

internal sealed class DistributedCacheAuthorizationInterceptor(IDistributedCache cache) : IAuthorizationInterceptor
{
    private const string CacheKey = "authorization_interceptor_distributed_cache_DistributedCacheAuthorizationInterceptor_{0}";

    public async ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken, string? cacheKeySuffix = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = MountCacheKey(name, cacheKeySuffix);

        var data = await cache.GetStringAsync(key, cancellationToken);

        return string.IsNullOrEmpty(data) ? null : AuthorizationHeadersJsonSerializer.Deserialize(data);
    }

    public async ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken, string? cacheKeySuffix = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (newHeaders == null)
            return;

        var key = MountCacheKey(name, cacheKeySuffix);

        var data = AuthorizationHeadersJsonSerializer.Serialize(newHeaders);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = newHeaders.GetRealExpiration()
        };

        await cache.SetStringAsync(key, data, options, cancellationToken);
    }

    private static string MountCacheKey(string name, string? cacheKeySuffix)
        => string.IsNullOrEmpty(cacheKeySuffix)
                    ? string.Format(CacheKey, name)
                    : $"{string.Format(CacheKey, name)}_{cacheKeySuffix}";
}
