using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Utils;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Strategies;

internal class AuthorizationInterceptorStrategy(ILoggerFactory loggerFactory, IAuthorizationInterceptor[] interceptors, KeyedAsyncLock authenticationLock)
    : IAuthorizationInterceptorStrategy
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("AuthorizationInterceptorStrategy");

    public async ValueTask<AuthorizationHeaders?> GetHeadersAsync(string key, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (interceptors.Length == 0)
        {
            _logger.LogNoInterceptorUsed(key);
            return await AuthenticateAsync(key, null, authenticationHandler, cancellationToken);
        }

        var lookup = await LookupHeadersAsync(key, cancellationToken);
        if (lookup.IsValid)
            return await UpdateHeadersInInterceptorsAsync(key, lookup.Index, lookup.Headers, cancellationToken);

        using var handle = await authenticationLock.AcquireAsync(key, cancellationToken);

        if (handle.WasContended)
        {
            var refreshed = await LookupHeadersAsync(key, cancellationToken);
            if (refreshed.IsValid)
            {
                _logger.LogHeadersRefreshedByConcurrentCaller(key);
                return await UpdateHeadersInInterceptorsAsync(key, refreshed.Index, refreshed.Headers, cancellationToken);
            }

            lookup = refreshed;
        }

        return await AuthenticateAsync(key, lookup.Headers, authenticationHandler, cancellationToken);
    }

    public async ValueTask<AuthorizationHeaders?> UpdateHeadersAsync(string key, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (interceptors.Length == 0)
            return await AuthenticateAsync(key, expiredHeaders, authenticationHandler, cancellationToken);

        using var handle = await authenticationLock.AcquireAsync(key, cancellationToken);

        if (handle.WasContended)
        {
            var refreshed = await LookupHeadersAsync(key, cancellationToken);
            if (refreshed.IsValid && IsNewerThan(refreshed.Headers, expiredHeaders))
            {
                _logger.LogHeadersRefreshedByConcurrentCaller(key);
                return await UpdateHeadersInInterceptorsAsync(key, refreshed.Index, refreshed.Headers, cancellationToken);
            }
        }

        return await AuthenticateAsync(key, expiredHeaders, authenticationHandler, cancellationToken);
    }

    /// <summary>
    /// Walks the interceptors in order and returns the first headers found, along with the index they
    /// came from and whether they are still valid. Expired headers are returned as well, so they can be
    /// handed to the authentication handler for a refresh token flow.
    /// </summary>
    private async ValueTask<HeadersLookup> LookupHeadersAsync(string key, CancellationToken cancellationToken)
    {
        for (var index = 0; index < interceptors.Length; index++)
        {
            try
            {
                LogDebug("Getting headers from interceptor '{interceptor}' with integration '{key}'", interceptors[index].GetType().Name, key);

                cancellationToken.ThrowIfCancellationRequested();

                var headers = await interceptors[index].GetHeadersAsync(key, cancellationToken);
                if (headers == null)
                    continue;

                LogDebug("Headers found in interceptor '{interceptor}' with integration '{key}'", interceptors[index].GetType().Name, key);

                if (headers.IsHeadersValid())
                {
                    LogDebug("Headers still valid in interceptor '{interceptor}' with integration '{key}'", interceptors[index].GetType().Name, key);
                    return new HeadersLookup(headers, index, true);
                }

                LogDebug("Headers is expired in interceptor '{interceptor}' with integration '{key}'", interceptors[index].GetType().Name, key);
                return new HeadersLookup(headers, index, false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogOperationCanceledInInterceptor(interceptors[index].GetType().Name, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting headers from interceptor '{interceptor}' with integration '{key}'", interceptors[index].GetType().Name, key);
            }
        }

        return new HeadersLookup(null, interceptors.Length, false);
    }

    private async ValueTask<AuthorizationHeaders?> AuthenticateAsync(string key, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
    {
        LogDebug("Getting new headers from AuthenticationHandler '{authenticationHandler}' with integration '{key}'", authenticationHandler.GetType().Name, key);

        cancellationToken.ThrowIfCancellationRequested();

        var newHeaders = await authenticationHandler.AuthenticateAsync(expiredHeaders, cancellationToken);
        if (newHeaders == null)
        {
            LogDebug("No new headers generated in AuthenticationHandler '{authenticationHandler}' with integration '{key}'", authenticationHandler.GetType().Name, key);
            return null;
        }

        return await UpdateHeadersInInterceptorsAsync(key, interceptors.Length, newHeaders, cancellationToken);
    }

    private async ValueTask<AuthorizationHeaders?> UpdateHeadersInInterceptorsAsync(string key, int startIndex, AuthorizationHeaders? headers, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (headers == null)
            return null;

        for (var index = startIndex - 1; index >= 0; index--)
        {
            try
            {
                LogDebug("Updating headers in interceptor '{interceptor}' with integration '{key}'", interceptors[index].GetType().Name, key);

                cancellationToken.ThrowIfCancellationRequested();

                await interceptors[index].UpdateHeadersAsync(key, null, headers, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogOperationCanceledInInterceptor(interceptors[index].GetType().Name, key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating headers in interceptor '{interceptor}' with integration '{key}'", interceptors[index].GetType().Name, key);
            }
        }

        return headers;
    }

    /// <summary>
    /// Tells whether <paramref name="cached"/> was authenticated after <paramref name="expiredHeaders"/>,
    /// meaning another caller already replaced the headers that were just rejected by the target API.
    /// </summary>
    private static bool IsNewerThan(AuthorizationHeaders? cached, AuthorizationHeaders? expiredHeaders)
        => cached != null && (expiredHeaders == null || cached.AuthenticatedAt > expiredHeaders.AuthenticatedAt);

    private void LogDebug(string message, params object[] parameters)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug(message, parameters);
    }

    private readonly record struct HeadersLookup(AuthorizationHeaders? Headers, int Index, bool IsValid);
}
