using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Utils;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Strategies;

internal class AuthorizationInterceptorStrategy(ILoggerFactory loggerFactory, IAuthorizationInterceptor[] interceptors, AuthenticationSingleFlight singleFlight)
    : IAuthorizationInterceptorStrategy
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("AuthorizationInterceptorStrategy");

    // Resolved once because the interceptor array is fixed: the log arguments are evaluated before the
    // call, so leaving GetType().Name inline would pay for it on every lookup even with logging off.
    private readonly string[] _interceptorNames = [.. interceptors.Select(interceptor => interceptor.GetType().Name)];

    public async ValueTask<AuthorizationHeaders?> GetHeadersAsync(string key, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (interceptors.Length == 0)
        {
            // Nothing caches the result, so coalescing callers would only queue identical
            // authentications instead of saving any.
            _logger.LogNoInterceptorUsed(key);
            return await AuthenticateAsync(key, null, authenticationHandler, cancellationToken);
        }

        var lookup = await LookupHeadersAsync(key, cancellationToken);
        if (lookup.IsValid)
            return await UpdateHeadersInInterceptorsAsync(key, lookup.Index, lookup.Headers, cancellationToken);

        var flight = await singleFlight.RunAsync(
            key,
            () => AuthenticateAsync(key, lookup.Headers, authenticationHandler, CancellationToken.None).AsTask(),
            cancellationToken);

        if (flight.Joined)
            _logger.LogHeadersRefreshedByConcurrentCaller(key);

        return flight.Headers;
    }

    public async ValueTask<AuthorizationHeaders?> UpdateHeadersAsync(string key, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (interceptors.Length == 0)
            return await AuthenticateAsync(key, expiredHeaders, authenticationHandler, cancellationToken);

        var flight = await singleFlight.RunAsync(
            key,
            () => AuthenticateAsync(key, expiredHeaders, authenticationHandler, CancellationToken.None).AsTask(),
            cancellationToken);

        if (flight.Joined)
            _logger.LogHeadersRefreshedByConcurrentCaller(key);

        // A joined flight may have started before this caller's headers were rejected, in which case it
        // produced the very generation the target API just refused, and only then is a fresh one needed.
        // A flight that produced nothing is a terminal answer rather than a stale one: retrying it per
        // caller would undo the coalescing precisely when the authentication provider is struggling.
        if (!flight.Joined || flight.Headers == null || IsNewerThan(flight.Headers, expiredHeaders))
            return flight.Headers;

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
                _logger.LogGettingHeadersFromInterceptor(_interceptorNames[index], key);

                cancellationToken.ThrowIfCancellationRequested();

                var headers = await interceptors[index].GetHeadersAsync(key, cancellationToken);
                if (headers == null)
                    continue;

                _logger.LogHeadersFoundInInterceptor(_interceptorNames[index], key);

                if (headers.IsHeadersValid())
                {
                    _logger.LogHeadersStillValidInInterceptor(_interceptorNames[index], key);
                    return new HeadersLookup(headers, index, true);
                }

                _logger.LogHeadersExpiredInInterceptor(_interceptorNames[index], key);
                return new HeadersLookup(headers, index, false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogOperationCanceledInInterceptor(_interceptorNames[index], key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogErrorGettingHeadersFromInterceptor(ex, _interceptorNames[index], key);
            }
        }

        return new HeadersLookup(null, interceptors.Length, false);
    }

    private async ValueTask<AuthorizationHeaders?> AuthenticateAsync(string key, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
    {
        _logger.LogGettingNewHeadersFromAuthenticationHandler(authenticationHandler.GetType().Name, key);

        cancellationToken.ThrowIfCancellationRequested();

        var newHeaders = await authenticationHandler.AuthenticateAsync(expiredHeaders, cancellationToken);
        if (newHeaders == null)
        {
            _logger.LogNoNewHeadersGenerated(authenticationHandler.GetType().Name, key);
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
                _logger.LogUpdatingHeadersInInterceptor(_interceptorNames[index], key);

                cancellationToken.ThrowIfCancellationRequested();

                await interceptors[index].UpdateHeadersAsync(key, null, headers, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogOperationCanceledInInterceptor(_interceptorNames[index], key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogErrorUpdatingHeadersInInterceptor(ex, _interceptorNames[index], key);
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

    private readonly record struct HeadersLookup(AuthorizationHeaders? Headers, int Index, bool IsValid);
}
