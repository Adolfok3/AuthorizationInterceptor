using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Strategies
{
    internal class AuthorizationInterceptorStrategy : IAuthorizationInterceptorStrategy
    {
        private readonly IAuthorizationInterceptor[] _interceptors;
        private readonly ILogger<AuthorizationInterceptorStrategy> _logger;

        public AuthorizationInterceptorStrategy(ILogger<AuthorizationInterceptorStrategy> logger, IEnumerable<IAuthorizationInterceptor> interceptors)
        {
            _logger = logger;
            _interceptors = interceptors.ToArray();
        }

        public async Task<AuthorizationHeaders?> GetHeadersAsync(string name, IAuthenticationHandler authenticationHandler)
        {
            AuthorizationHeaders? headers;
            int index;

            for (index = 0; index < _interceptors.Length; index++)
            {
                try
                {
                    LogDebug("Getting headers from interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);
                    headers = await _interceptors[index].GetHeadersAsync(name);
                    if (headers != null)
                        return await UpdateHeadersInInterceptorsAsync(name, index, headers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting headers from interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);
                }
            }

            LogDebug("Getting headers from AuthenticationHandler '{authenticationHandler}' with integration '{name}'", authenticationHandler.GetType().Name, name);

            headers = await authenticationHandler.AuthenticateAsync();
            return await UpdateHeadersInInterceptorsAsync(name, index, headers);
        }

        public async Task<AuthorizationHeaders?> UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler)
        {
            LogDebug("Getting new headers from AuthenticationHandler '{authenticationHandler}' with integration '{name}'", authenticationHandler.GetType().Name, name);
            var newHeaders = await authenticationHandler.UnauthenticateAsync(expiredHeaders);
            if (newHeaders == null)
                return null;

            return await UpdateHeadersInInterceptorsAsync(name, _interceptors.Length, newHeaders);
        }

        private async Task<AuthorizationHeaders?> UpdateHeadersInInterceptorsAsync(string name, int startIndex, AuthorizationHeaders? headers = null)
        {
            if (headers == null)
                return null;

            for (var index = startIndex - 1; index >= 0; index--)
            {
                try
                {
                    LogDebug("Updating headers in interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);
                    await _interceptors[index].UpdateHeadersAsync(name, null, headers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating headers in interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);
                }
            }

            return headers;
        }

        private void LogDebug(string message, params object[] parameters)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(message, parameters);
        }
    }
}
