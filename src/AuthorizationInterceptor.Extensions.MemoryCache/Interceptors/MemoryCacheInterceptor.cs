using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Extensions.MemoryCache.Interceptors
{
    internal class MemoryCacheInterceptor : IAuthorizationInterceptor
    {
        private const string CacheKey = "authorization_interceptor_memory_cache_MemoryCacheInterceptor_{0}";
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheInterceptor(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var headers = _memoryCache.Get<AuthorizationHeaders?>(string.Format(CacheKey, name));
            return new(headers);
        }

        public ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (newHeaders == null)
                return ValueTask.CompletedTask;

            _memoryCache.Set(string.Format(CacheKey, name), newHeaders, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = newHeaders.GetRealExpiration(),
                Priority = CacheItemPriority.NeverRemove
            });

            return ValueTask.CompletedTask;
        }
    }
}
