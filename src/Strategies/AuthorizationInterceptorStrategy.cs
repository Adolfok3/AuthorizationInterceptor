using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Strategies
{
    internal class AuthorizationInterceptorStrategy : IAuthorizationInterceptorStrategy
    {
        private readonly IAuthorizationInterceptor[] _interceptors;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly ILogger _logger;
        private readonly string _authenticationHandlerName;

        public AuthorizationInterceptorStrategy(IAuthenticationHandler authenticationHandler, ILoggerFactory loggerFactory, IAuthorizationInterceptor[] interceptors)
        {
            _authenticationHandler = authenticationHandler;
            _interceptors = interceptors;
            _logger = loggerFactory.CreateLogger(nameof(AuthorizationInterceptorStrategy));
            _authenticationHandlerName = _authenticationHandler.GetType().Name;
        }

        public async Task<AuthorizationHeaders?> GetHeadersAsync()
        {
            AuthorizationHeaders? headers;
            int index;

            for (index = 0; index < _interceptors.Length; index++)
            {
                try
                {
                    Log("Getting headers from {interceptor}", _interceptors[index].GetType().Name);
                    headers = await _interceptors[index].GetHeadersAsync();
                    if (headers != null)
                        return await UpdateHeadersInInterceptorsAsync(index, headers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on getting headers from {interceptor}", _interceptors[index].GetType().Name);
                }
            }

            Log("Getting headers from {authenticationHandler}", _authenticationHandlerName);

            headers = await _authenticationHandler.AuthenticateAsync();
            return await UpdateHeadersInInterceptorsAsync(index, headers);
        }

        public async Task<AuthorizationHeaders?> UpdateHeadersAsync(AuthorizationHeaders? expiredHeaders)
        {
            Log("Getting new headers from {authenticationHandler}", _authenticationHandlerName);
            var newHeaders = await _authenticationHandler.UnauthenticateAsync(expiredHeaders);
            if (newHeaders == null)
                return null;

            return await UpdateHeadersInInterceptorsAsync(_interceptors.Length, newHeaders);
        }

        private async Task<AuthorizationHeaders?> UpdateHeadersInInterceptorsAsync(int startIndex, AuthorizationHeaders? headers = null)
        {
            if (headers == null)
                return null;

            for (int index = startIndex - 1; index >= 0; index--)
            {
                try
                {
                    Log("Updating headers in {interceptor}", _interceptors[index].GetType().Name);
                    await _interceptors[index].UpdateHeadersAsync(null, headers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on updating headers in {interceptor}", _interceptors[index].GetType().Name);
                }
            }

            return headers;
        }

        private void Log(string message, params object[] parameters)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(message, parameters);
        }
    }
}
