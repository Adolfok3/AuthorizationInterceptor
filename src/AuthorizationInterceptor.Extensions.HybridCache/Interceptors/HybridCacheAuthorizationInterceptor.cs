using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.Abstractions.Json;
using Microsoft.Extensions.Caching.Hybrid;

namespace AuthorizationInterceptor.Extensions.HybridCache.Interceptors;

internal sealed class HybridCacheAuthorizationInterceptor(Microsoft.Extensions.Caching.Hybrid.HybridCache hybridCache) : IAuthorizationInterceptor
{
    private const string CacheKey = "authorization_interceptor_hybrid_cache_HybridCacheAuthorizationInterceptor_{0}";

    public async ValueTask<AuthorizationHeaders?> GetHeadersAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var data = await hybridCache.GetOrCreateAsync(string.Format(CacheKey, key),
            _ => ValueTask.FromResult<string?>(null),
            new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite },
            cancellationToken: cancellationToken);

        return string.IsNullOrEmpty(data) ? null : AuthorizationHeadersJsonSerializer.Deserialize(data);
    }

    public async ValueTask UpdateHeadersAsync(string key, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (newHeaders == null)
            return;

        var data = AuthorizationHeadersJsonSerializer.Serialize(newHeaders);
        var options = new HybridCacheEntryOptions
        {
            Expiration = newHeaders.GetRealExpiration(),
            LocalCacheExpiration = newHeaders.GetRealExpiration(),
        };

        await hybridCache.SetAsync(string.Format(CacheKey, key), data, options, cancellationToken: cancellationToken);
    }
}
