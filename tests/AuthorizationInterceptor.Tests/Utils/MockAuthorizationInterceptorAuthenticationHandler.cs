using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Handlers;

namespace AuthorizationInterceptor.Tests.Utils
{
    public class MockAuthorizationInterceptorAuthenticationHandler : IAuthenticationHandler
    {
        public Task<AuthorizationEntry> AuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        public Task<AuthorizationEntry> UnauthenticateAsync(AuthorizationEntry? entries)
        {
            throw new NotImplementedException();
        }
    }
}
