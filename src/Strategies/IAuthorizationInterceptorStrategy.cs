using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Strategies
{
    internal interface IAuthorizationInterceptorStrategy
    {
        Task<AuthorizationHeaders?> GetHeadersAsync(string name, IAuthenticationHandler authenticationHandler);

        Task<AuthorizationHeaders?> UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, IAuthenticationHandler authenticationHandler);
    }
}
