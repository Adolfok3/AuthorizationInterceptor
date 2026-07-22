using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;

namespace AuthorizationInterceptor.Tests.Utils;

/// <summary>
/// Dependency that an interceptor can only get through the registration callback of
/// <c>UseCustomInterceptor</c>.
/// </summary>
public class MockInterceptorDependency;

/// <summary>
/// Interceptor that cannot be activated unless <see cref="MockInterceptorDependency"/> was registered
/// before the service provider was built.
/// </summary>
public class MockDependentAuthorizationInterceptor(MockInterceptorDependency dependency) : IAuthorizationInterceptor
{
    public MockInterceptorDependency Dependency => dependency;

    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken, string? cacheKeySuffix = null)
        => ValueTask.FromResult<AuthorizationHeaders?>(null);

    public ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken, string? cacheKeySuffix = null)
        => ValueTask.CompletedTask;
}
