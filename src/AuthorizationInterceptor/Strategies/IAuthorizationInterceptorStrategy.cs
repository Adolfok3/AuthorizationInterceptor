using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;

namespace AuthorizationInterceptor.Strategies;

internal interface IAuthorizationInterceptorStrategy
{
    ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, IAuthenticationHandler authenticationHandler, string? cacheKeySuffix, CancellationToken cancellationToken);

    ValueTask<AuthorizationHeaders?> UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, string? cacheKeySuffix, CancellationToken cancellationToken);
}
