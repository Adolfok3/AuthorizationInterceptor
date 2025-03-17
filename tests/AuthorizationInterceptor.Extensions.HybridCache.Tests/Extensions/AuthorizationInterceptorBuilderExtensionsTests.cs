using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.HybridCache.Extensions;
using AuthorizationInterceptor.Extensions.HybridCache.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.Extensions.HybridCache.Tests.Extensions;

public class AuthorizationInterceptorBuilderExtensionsTests
{
    [Fact]
    public void UseDistributedCache_ShouldRegisterDistributedCacheDependencies_AndAddInterceptor()
    {
        // Arrange
        var options = Substitute.For<IAuthorizationInterceptorOptions>();

        // Act
        options.UseHybridCacheInterceptor();

        // Assert
        options.Received(1).UseCustomInterceptor<HybridCacheAuthorizationInterceptor>(Arg.Any<Func<IServiceCollection, IServiceCollection>?>());
    }
}
