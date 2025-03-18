using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Log;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Strategies;

internal class AuthorizationInterceptorStrategy(ILoggerFactory loggerFactory, IAuthorizationInterceptor[] interceptors)
    : IAuthorizationInterceptorStrategy
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("AuthorizationInterceptorStrategy");

    public async ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AuthorizationHeaders? headers = null;
        int index;

        for (index = 0; index < interceptors.Length; index++)
        {
            try
            {
                LogDebug("Getting headers from interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);

                cancellationToken.ThrowIfCancellationRequested();

                headers = await interceptors[index].GetHeadersAsync(name, cancellationToken);
                if (headers == null)
                    continue;

                LogDebug("Headers found in interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);

                if (headers.IsHeadersValid())
                {
                    LogDebug("Headers still valid in interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);
                    return await UpdateHeadersInInterceptorsAsync(name, index, headers, cancellationToken);
                }

                LogDebug("Headers is expired in interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.LogOperationCanceledInInterceptor(interceptors[index].GetType().Name, name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting headers from interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);
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

        return await UpdateHeadersInInterceptorsAsync(name, interceptors.Length, newHeaders, cancellationToken);
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
                LogDebug("Updating headers in interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);

                cancellationToken.ThrowIfCancellationRequested();

                await interceptors[index].UpdateHeadersAsync(name, null, headers, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogOperationCanceledInInterceptor(interceptors[index].GetType().Name, name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating headers in interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);
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
