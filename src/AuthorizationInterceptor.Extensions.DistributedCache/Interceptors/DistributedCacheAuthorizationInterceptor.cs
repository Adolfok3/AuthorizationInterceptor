using System.Globalization;
using System.Text;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.Abstractions.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace AuthorizationInterceptor.Extensions.DistributedCache.Interceptors;

internal sealed class DistributedCacheAuthorizationInterceptor(IDistributedCache cache) : IAuthorizationInterceptor
{
    private readonly CompositeFormat keyFormat = CompositeFormat.Parse("authorization_interceptor_distributed_cache_DistributedCacheAuthorizationInterceptor_{0}");

    public async ValueTask<AuthorizationHeaders?> GetHeadersAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var data = await cache.GetStringAsync(string.Format(null, keyFormat, key), cancellationToken);

        return string.IsNullOrEmpty(data) ? null : AuthorizationHeadersJsonSerializer.Deserialize(data);
    }

    public async ValueTask UpdateHeadersAsync(string key, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (newHeaders == null)
            return;

        var data = AuthorizationHeadersJsonSerializer.Serialize(newHeaders);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = newHeaders.GetRealExpiration(),
        };

        await cache.SetStringAsync(string.Format(null, keyFormat, key), data, options, cancellationToken);
    }
}
