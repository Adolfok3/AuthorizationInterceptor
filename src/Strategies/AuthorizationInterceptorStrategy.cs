using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public async ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AuthorizationHeaders? headers = null;
            int index;

            for (index = 0; index < _interceptors.Length; index++)
            {
                try
                {
                    LogDebug("Getting headers from interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);

                    cancellationToken.ThrowIfCancellationRequested();

                    headers = await _interceptors[index].GetHeadersAsync(name, cancellationToken);
                    if (headers == null)
                        continue;

                    LogDebug("Headers found in interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);

                    if (headers.IsHeadersValid())
                    {
                        LogDebug("Headers still valid in interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);
                        return await UpdateHeadersInInterceptorsAsync(name, index, headers, cancellationToken);
                    }

                    LogDebug("Headers is expired in interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);
                    break;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Operation canceled while getting headers from interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting headers from interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);
                }
            }

            return await UpdateHeadersAsync(name, headers, authenticationHandler, cancellationToken);
        }

        public async ValueTask<AuthorizationHeaders?> UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        {
            LogDebug("Getting new headers from AuthenticationHandler '{authenticationHandler}' with integration '{name}'", authenticationHandler.GetType().Name, name);

            cancellationToken.ThrowIfCancellationRequested();

            var newHeaders = await authenticationHandler.AuthenticateAsync(expiredHeaders, cancellationToken);
            if (newHeaders == null)
            {
                LogDebug("No new headers generated in AuthenticationHandler '{authenticationHandler}' with integration '{name}'", authenticationHandler.GetType().Name, name);
                return null;
            }

            return await UpdateHeadersInInterceptorsAsync(name, _interceptors.Length, newHeaders, cancellationToken);
        }

        private async ValueTask<AuthorizationHeaders?> UpdateHeadersInInterceptorsAsync(string name, int startIndex, AuthorizationHeaders? headers, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (headers == null)
                return null;

            for (var index = startIndex - 1; index >= 0; index--)
            {
                try
                {
                    LogDebug("Updating headers in interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);

                    cancellationToken.ThrowIfCancellationRequested();

                    await _interceptors[index].UpdateHeadersAsync(name, null, headers, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Operation canceled while updating headers in interceptor '{interceptor}' with integration '{name}'", _interceptors[index].GetType().Name, name);
                    throw;
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
