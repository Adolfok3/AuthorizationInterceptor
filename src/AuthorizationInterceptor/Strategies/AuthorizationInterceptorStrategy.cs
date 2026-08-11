using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Utils;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Strategies;

internal class AuthorizationInterceptorStrategy(ILoggerFactory loggerFactory, IAuthorizationInterceptor[] interceptors, AuthenticationLock authenticationLock)
    : IAuthorizationInterceptorStrategy
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("AuthorizationInterceptorStrategy");

    private readonly string[] _interceptorNames = [.. interceptors.Select(interceptor => interceptor.GetType().Name)];

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

        var flight = await RunWithLockAsync(key, lookup.Headers, authenticationHandler, cancellationToken);

        if (flight.Joined)
            _logger.LogHeadersRefreshedByConcurrentCaller(key);

        return flight.Headers;
    }

    public async ValueTask<AuthorizationHeaders?> UpdateHeadersAsync(string key, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (interceptors.Length == 0)
            return await AuthenticateAsync(key, expiredHeaders, authenticationHandler, cancellationToken);

        var flight = await RunWithLockAsync(key, expiredHeaders, authenticationHandler, cancellationToken);

        if (flight.Joined)
            _logger.LogHeadersRefreshedByConcurrentCaller(key);

        if (!flight.Joined || flight.Headers == null || IsNewerThan(flight.Headers, expiredHeaders))
            return flight.Headers;

        return await AuthenticateAsync(key, expiredHeaders, authenticationHandler, cancellationToken);
    }

    private ValueTask<AuthenticationLock.AuthenticationLockResult> RunWithLockAsync(string key, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken)
        => authenticationLock.RunAsync(
            key,
            ct => AuthenticateAsync(key, expiredHeaders, authenticationHandler, ct).AsTask(),
            ct => RevalidateFromConcurrentInstanceAsync(key, expiredHeaders, ct).AsTask(),
            cancellationToken);

    /// <summary>
    /// Re-checks the interceptors after the distributed lock is acquired. If a concurrent instance already
    /// refreshed the shared cache with newer headers, adopts and back-fills them so authentication is skipped.
    /// Returns <c>null</c> when a fresh authentication is still required.
    /// </summary>
    private async ValueTask<AuthorizationHeaders?> RevalidateFromConcurrentInstanceAsync(string key, AuthorizationHeaders? expiredHeaders, CancellationToken cancellationToken)
    {
        var lookup = await LookupHeadersAsync(key, cancellationToken);
        if (lookup.IsValid && IsNewerThan(lookup.Headers, expiredHeaders))
        {
            _logger.LogHeadersRefreshedByConcurrentInstance(key);
            return await UpdateHeadersInInterceptorsAsync(key, lookup.Index, lookup.Headers, cancellationToken);
        }

        return null;
    }

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

    private static bool IsNewerThan(AuthorizationHeaders? cached, AuthorizationHeaders? expiredHeaders)
        => cached != null && (expiredHeaders == null || cached.AuthenticatedAt > expiredHeaders.AuthenticatedAt);

    private readonly record struct HeadersLookup(AuthorizationHeaders? Headers, int Index, bool IsValid);
}
