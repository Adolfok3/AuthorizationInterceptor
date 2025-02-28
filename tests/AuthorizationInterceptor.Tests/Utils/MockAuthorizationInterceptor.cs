using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;

namespace AuthorizationInterceptor.Tests.Utils
{
    public class MockAuthorizationInterceptor : IAuthorizationInterceptor
    {
        public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<AuthorizationHeaders?>(null);
        }

        public ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
