using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;

namespace AuthorizationInterceptor.Strategies;

internal interface IAuthorizationInterceptorStrategy
{
    ValueTask<AuthorizationHeaders?> GetHeadersAsync(string key, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken);

    ValueTask<AuthorizationHeaders?> UpdateHeadersAsync(string key, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken);
}
