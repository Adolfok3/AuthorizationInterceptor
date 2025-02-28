using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;

namespace AuthorizationInterceptor.Tests.Utils
{
    public class MockAuthorizationInterceptorAuthenticationHandler : IAuthenticationHandler
    {
        public ValueTask<AuthorizationHeaders?> AuthenticateAsync(AuthorizationHeaders? expiredHeaders, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
