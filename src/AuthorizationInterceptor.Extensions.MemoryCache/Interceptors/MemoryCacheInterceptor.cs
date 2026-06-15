using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using Microsoft.Extensions.Caching.Memory;

namespace AuthorizationInterceptor.Extensions.MemoryCache.Interceptors;

internal class MemoryCacheInterceptor(IMemoryCache memoryCache) : IAuthorizationInterceptor
{
    private const string CacheKey = "authorization_interceptor_memory_cache_MemoryCacheInterceptor_{0}";

    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken, string? cacheKeySuffix = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = MountCacheKey(name, cacheKeySuffix);

        var headers = memoryCache.Get<AuthorizationHeaders?>(key);

        return new ValueTask<AuthorizationHeaders?>(headers);
    }

    private static string MountCacheKey(string name, string? cacheKeySuffix)
    {
        return string.IsNullOrEmpty(cacheKeySuffix)
                    ? string.Format(CacheKey, name)
                    : $"{string.Format(CacheKey, name)}_{cacheKeySuffix}";
    }

    public ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken, string? cacheKeySuffix = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (newHeaders == null)
            return ValueTask.CompletedTask;

        var key = MountCacheKey(name, cacheKeySuffix);

        memoryCache.Set(key, newHeaders, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = newHeaders.GetRealExpiration(),
            Priority = CacheItemPriority.NeverRemove
        });

        return ValueTask.CompletedTask;
    }
}
