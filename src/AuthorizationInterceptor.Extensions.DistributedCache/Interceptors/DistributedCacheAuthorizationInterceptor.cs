using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.Abstractions.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Extensions.DistributedCache.Interceptors
{
    internal class DistributedCacheAuthorizationInterceptor : IAuthorizationInterceptor
    {
        private readonly IDistributedCache _cache;
        private const string CacheKey = "authorization_interceptor_distributed_cache_DistributedCacheAuthorizationInterceptor_{0}";

        public DistributedCacheAuthorizationInterceptor(IDistributedCache cache)
            => _cache = cache;

        public async ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken)
        {
            var data = await _cache.GetStringAsync(string.Format(CacheKey, name), cancellationToken);
            return string.IsNullOrEmpty(data) ? null : AuthorizationHeadersJsonSerializer.Deserialize(data);
        }

        public async ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
        {
            if (newHeaders == null)
                return;

            var data = AuthorizationHeadersJsonSerializer.Serialize(newHeaders);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = newHeaders.GetRealExpiration()
            };

            await _cache.SetStringAsync(string.Format(CacheKey, name), data, options, cancellationToken);
        }
    }
}
