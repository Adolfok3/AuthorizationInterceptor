using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Strategies
{
    internal interface IAuthorizationInterceptorStrategy
    {
        ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken);

        ValueTask<AuthorizationHeaders?> UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken);
    }
}
