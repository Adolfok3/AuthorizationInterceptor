using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;

namespace AuthorizationInterceptor.Tests.Utils
{
    public class MockAuthorizationInterceptor : IAuthorizationInterceptor
    {
        public Task<AuthorizationHeaders?> GetHeadersAsync(string name)
        {
            return Task.FromResult<AuthorizationHeaders?>(null);
        }

        public Task UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders)
        {
            return Task.FromResult<AuthorizationHeaders?>(null);
        }
    }
}
