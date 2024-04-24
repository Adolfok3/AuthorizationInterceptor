using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;

namespace AuthorizationInterceptor.Tests.Utils
{
    public class MockAuthorizationInterceptorAuthenticationHandler : IAuthenticationHandler
    {
        public Task<AuthorizationHeaders> AuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        public Task<AuthorizationHeaders> UnauthenticateAsync(AuthorizationHeaders? headers)
        {
            throw new NotImplementedException();
        }
    }
}
