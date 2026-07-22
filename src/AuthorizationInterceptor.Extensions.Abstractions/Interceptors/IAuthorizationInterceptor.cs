using AuthorizationInterceptor.Extensions.Abstractions.Headers;

namespace AuthorizationInterceptor.Extensions.Abstractions.Interceptors;

/// <summary>
/// Defines the interface for interceptor components responsible for managing authorization headers.
/// </summary>
public interface IAuthorizationInterceptor
{
    /// <summary>
    /// Retrieves the current set of authorization headers in current interceptor.
    /// </summary>
    /// <param name="key">Identifies the authorization headers being handled. It is the name of the integration or HttpClient,
    /// already combined with the suffix produced by <c>CacheKeyBuilder</c> when one is configured.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <see cref="AuthorizationHeaders"/> containing the authorization headers.
    /// </returns>
    ValueTask<AuthorizationHeaders?> GetHeadersAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Update the current set of authorization headers in current interceptor.
    /// </summary>
    /// <param name="key">Identifies the authorization headers being handled. It is the name of the integration or HttpClient,
    /// already combined with the suffix produced by <c>CacheKeyBuilder</c> when one is configured.</param>
    /// <param name="expiredHeaders">The old expired headers</param>
    /// <param name="newHeaders">The new valid headers</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask UpdateHeadersAsync(string key, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken);
}
