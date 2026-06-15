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
    /// <param name="name">Name of the integration or HttpClient</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="cacheKeySuffix">Optional suffix to differentiate cache keys (e.g., by user). If null or empty, no suffix is applied.</param>
    /// <returns>
    /// <see cref="AuthorizationHeaders"/> containing the authorization headers.
    /// </returns>
    ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken, string? cacheKeySuffix = null);

    /// <summary>
    /// Update the current set of authorization headers in current interceptor.
    /// </summary>
    /// <param name="name">Name of the integration or HttpClient</param>
    /// <param name="expiredHeaders">The old expired headers</param>
    /// <param name="newHeaders">The new valid headers</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="cacheKeySuffix">Optional suffix to differentiate cache keys (e.g., by user). If null or empty, no suffix is applied.</param>
    ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken, string? cacheKeySuffix = null);
}
