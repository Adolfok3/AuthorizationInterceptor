using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Handlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Interceptors
{
    internal class MemoryAuthorizationInterceptor : AuthorizationInterceptorBase
    {
        private readonly IMemoryCache _memoryCache;
        private readonly string _cacheKey;

        public MemoryAuthorizationInterceptor(IAuthenticationHandler authenticationHandler, IMemoryCache memoryCache, ILogger<MemoryAuthorizationInterceptor> logger, IAuthorizationInterceptor? nextInterceptor = null) : base("MemoryCache", authenticationHandler, logger, nextInterceptor)
        {
            _memoryCache = memoryCache;
            _cacheKey = $"authorization_interceptor_memory_cache_{GetAuthenticationHandlerName()}";
        }

        protected override async Task<AuthorizationEntry> OnGetHeadersAsync()
            => await GetOrCreateHeadersAsync();

        protected override async Task<AuthorizationEntry> OnUpdateHeadersAsync(AuthorizationEntry expiredEntries)
        {
            _memoryCache.Remove(_cacheKey);
            var newHeaders = await base.OnUpdateHeadersAsync(expiredEntries);
            Log("Setting new headers on MemoryCache");

            return await GetOrCreateHeadersAsync(newHeaders);
        }

        private async Task<AuthorizationEntry> GetOrCreateHeadersAsync(AuthorizationEntry? entry = null)
        {
            var headers = await _memoryCache.GetOrCreateAsync(_cacheKey, async factory =>
            {
                var headers = entry ?? await base.OnGetHeadersAsync();
                factory.AbsoluteExpirationRelativeToNow = GetRealExpiration(headers.ExpiresIn, headers.AuthenticatedAt);
                factory.Priority = CacheItemPriority.NeverRemove;
                return headers;
            });

            return headers!;
        }

        private TimeSpan? GetRealExpiration(TimeSpan? expiresIn, DateTimeOffset authenticatedAt)
            => expiresIn - (DateTimeOffset.UtcNow - authenticatedAt);
    }
}
