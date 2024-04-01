using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Handlers;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Interceptors
{
    public abstract class AuthorizationInterceptorBase : IAuthorizationInterceptor
    {
        protected readonly ILogger<AuthorizationInterceptorBase> Logger;

        private readonly string _interceptor;
        private readonly IAuthorizationInterceptor? _nextInterceptor;
        private readonly IAuthenticationHandler _authenticationHandler;

        protected AuthorizationInterceptorBase(string interceptor, IAuthenticationHandler authenticationHandler, ILogger<AuthorizationInterceptorBase> logger, IAuthorizationInterceptor? nextInterceptor = null)
        {
            Logger = logger;
            _interceptor = interceptor;
            _nextInterceptor = nextInterceptor;
            _authenticationHandler = authenticationHandler;
        }

        public async Task<AuthorizationEntry> GetHeadersAsync()
        {
            Log("Getting headers on {interceptor} interceptor", _interceptor);
            return await OnGetHeadersAsync();
        }

        public async Task<AuthorizationEntry> UpdateHeadersAsync(AuthorizationEntry expiredEntries)
        {
            Log("Updating headers on {interceptor} interceptor", _interceptor);
            return await OnUpdateHeadersAsync(expiredEntries);
        }

        protected virtual async Task<AuthorizationEntry> OnGetHeadersAsync()
        {
            if (_nextInterceptor == null)
            {
                Log("Getting headers from Authentication handler");
                return await _authenticationHandler.AuthenticateAsync();
            }

            return await _nextInterceptor.GetHeadersAsync();
        }

        protected virtual async Task<AuthorizationEntry> OnUpdateHeadersAsync(AuthorizationEntry expiredEntries)
        {
            if (_nextInterceptor != null)
                return await _nextInterceptor.UpdateHeadersAsync(expiredEntries);

            Log("Getting new headers from Authentication handler");
            return await _authenticationHandler.UnauthenticateAsync(expiredEntries);
        }

        protected string GetAuthenticationHandlerName()
        {
            return _authenticationHandler.GetType().Name;
        }

        protected void Log(string message, params object[] objects)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.LogDebug(message, objects);
        }
    }
}
