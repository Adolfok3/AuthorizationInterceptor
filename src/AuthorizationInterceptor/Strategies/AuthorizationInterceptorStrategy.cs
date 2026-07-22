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

    public async ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, IAuthenticationHandler authenticationHandler, string? cacheKeySuffix, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (interceptors.Length == 0)
        {
            _logger.LogNoInterceptorUsed(name);
            return await AuthenticateAsync(name, null, authenticationHandler, cacheKeySuffix, cancellationToken);
        }

        var lookup = await LookupHeadersAsync(name, cacheKeySuffix, cancellationToken);
        if (lookup.IsValid)
            return await UpdateHeadersInInterceptorsAsync(name, lookup.Index, lookup.Headers, cacheKeySuffix, cancellationToken);

        using var handle = await authenticationLock.AcquireAsync(BuildLockKey(name, cacheKeySuffix), cancellationToken);

        if (handle.WasContended)
        {
            var refreshed = await LookupHeadersAsync(name, cacheKeySuffix, cancellationToken);
            if (refreshed.IsValid)
            {
                _logger.LogHeadersRefreshedByConcurrentCaller(name, cacheKeySuffix);
                return await UpdateHeadersInInterceptorsAsync(name, refreshed.Index, refreshed.Headers, cacheKeySuffix, cancellationToken);
            }

            lookup = refreshed;
        }

        return await AuthenticateAsync(name, lookup.Headers, authenticationHandler, cacheKeySuffix, cancellationToken);
    }

    public async ValueTask<AuthorizationHeaders?> UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, string? cacheKeySuffix, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (interceptors.Length == 0)
            return await AuthenticateAsync(name, expiredHeaders, authenticationHandler, cacheKeySuffix, cancellationToken);

        using var handle = await authenticationLock.AcquireAsync(BuildLockKey(name, cacheKeySuffix), cancellationToken);

        if (handle.WasContended)
        {
            var refreshed = await LookupHeadersAsync(name, cacheKeySuffix, cancellationToken);
            if (refreshed.IsValid && IsNewerThan(refreshed.Headers, expiredHeaders))
            {
                _logger.LogHeadersRefreshedByConcurrentCaller(name, cacheKeySuffix);
                return await UpdateHeadersInInterceptorsAsync(name, refreshed.Index, refreshed.Headers, cacheKeySuffix, cancellationToken);
            }
        }

        return await AuthenticateAsync(name, expiredHeaders, authenticationHandler, cacheKeySuffix, cancellationToken);
    }

    /// <summary>
    /// Walks the interceptors in order and returns the first headers found, along with the index they
    /// came from and whether they are still valid. Expired headers are returned as well, so they can be
    /// handed to the authentication handler for a refresh token flow.
    /// </summary>
    private async ValueTask<HeadersLookup> LookupHeadersAsync(string name, string? cacheKeySuffix, CancellationToken cancellationToken)
    {
        for (var index = 0; index < interceptors.Length; index++)
        {
            try
            {
                LogDebug("Getting headers from interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);

                cancellationToken.ThrowIfCancellationRequested();

                var headers = await interceptors[index].GetHeadersAsync(name, cancellationToken, cacheKeySuffix);
                if (headers == null)
                    continue;

                LogDebug("Headers found in interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);

                if (headers.IsHeadersValid())
                {
                    LogDebug("Headers still valid in interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);
                    return new HeadersLookup(headers, index, true);
                }

                LogDebug("Headers is expired in interceptor '{interceptor}' with integration '{name}'", interceptors[index].GetType().Name, name);
                return new HeadersLookup(headers, index, false);
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

        return new HeadersLookup(null, interceptors.Length, false);
    }

    private async ValueTask<AuthorizationHeaders?> AuthenticateAsync(string name, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, string? cacheKeySuffix, CancellationToken cancellationToken)
    {
        LogDebug("Getting new headers from AuthenticationHandler '{authenticationHandler}' with integration '{name}'", authenticationHandler.GetType().Name, name);

        cancellationToken.ThrowIfCancellationRequested();

        var newHeaders = await authenticationHandler.AuthenticateAsync(expiredHeaders, cancellationToken);
        if (newHeaders == null)
        {
            LogDebug("No new headers generated in AuthenticationHandler '{authenticationHandler}' with integration '{name}'", authenticationHandler.GetType().Name, name);
            return null;
        }

        return await UpdateHeadersInInterceptorsAsync(name, interceptors.Length, newHeaders, cacheKeySuffix, cancellationToken);
    }

    private async ValueTask<AuthorizationHeaders?> UpdateHeadersInInterceptorsAsync(string name, int startIndex, AuthorizationHeaders? headers, string? cacheKeySuffix, CancellationToken cancellationToken)
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

                await interceptors[index].UpdateHeadersAsync(name, null, headers, cancellationToken, cacheKeySuffix);
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

    /// <summary>
    /// Tells whether <paramref name="cached"/> was authenticated after <paramref name="expiredHeaders"/>,
    /// meaning another caller already replaced the headers that were just rejected by the target API.
    /// </summary>
    private static bool IsNewerThan(AuthorizationHeaders? cached, AuthorizationHeaders? expiredHeaders)
        => cached != null && (expiredHeaders == null || cached.AuthenticatedAt > expiredHeaders.AuthenticatedAt);

    private static string BuildLockKey(string name, string? cacheKeySuffix)
        => string.IsNullOrEmpty(cacheKeySuffix) ? name : $"{name}_{cacheKeySuffix}";

    private void LogDebug(string message, params object[] parameters)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug(message, parameters);
    }

    private readonly record struct HeadersLookup(AuthorizationHeaders? Headers, int Index, bool IsValid);
}
