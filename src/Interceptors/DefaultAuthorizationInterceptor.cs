using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Handlers;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Interceptors
{
    internal class DefaultAuthorizationInterceptor : AuthorizationInterceptorBase
    {
        private const string WARNNING_MSG = "No custom interceptor has been configured. The recommendation is to at least use 'MemoryCache' to handle header authorization cache.";
        private readonly ILogger<DefaultAuthorizationInterceptor> _logger;

        public DefaultAuthorizationInterceptor(IAuthenticationHandler authenticationHandler, ILogger<DefaultAuthorizationInterceptor> logger, IAuthorizationInterceptor? nextInterceptor = null) : base("DEFAULT", authenticationHandler, logger, nextInterceptor)
        {
            _logger = logger;
        }

        protected override Task<AuthorizationEntry> OnGetHeadersAsync()
        {
            _logger.LogWarning(WARNNING_MSG);
            return base.OnGetHeadersAsync();
        }

        protected override Task<AuthorizationEntry> OnUpdateHeadersAsync(AuthorizationEntry expiredEntries)
        {
            _logger.LogWarning(WARNNING_MSG);
            return base.OnUpdateHeadersAsync(expiredEntries);
        }
    }
}
