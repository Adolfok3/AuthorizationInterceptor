using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Strategies
{
    internal interface IAuthorizationInterceptorStrategy
    {
        Task<AuthorizationHeaders?> GetHeadersAsync();

        Task<AuthorizationHeaders?> UpdateHeadersAsync(AuthorizationHeaders? expiredHeaders);
    }
}
