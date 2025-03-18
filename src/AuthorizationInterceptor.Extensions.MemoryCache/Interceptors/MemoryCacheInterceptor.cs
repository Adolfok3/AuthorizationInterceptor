using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using Microsoft.Extensions.Caching.Memory;

namespace AuthorizationInterceptor.Extensions.MemoryCache.Interceptors;

internal class MemoryCacheInterceptor(IMemoryCache memoryCache) : IAuthorizationInterceptor
{
    private const string CacheKey = "authorization_interceptor_memory_cache_MemoryCacheInterceptor_{0}";

    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var headers = memoryCache.Get<AuthorizationHeaders?>(string.Format(CacheKey, name));
        return new ValueTask<AuthorizationHeaders?>(headers);
    }

    public ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (newHeaders == null)
            return ValueTask.CompletedTask;

        memoryCache.Set(string.Format(CacheKey, name), newHeaders, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = newHeaders.GetRealExpiration(),
            Priority = CacheItemPriority.NeverRemove
        });

        return ValueTask.CompletedTask;
    }
}
