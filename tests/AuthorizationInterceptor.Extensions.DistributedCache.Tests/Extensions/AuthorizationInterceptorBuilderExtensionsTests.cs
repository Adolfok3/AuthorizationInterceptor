using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.DistributedCache.Extensions;
using AuthorizationInterceptor.Extensions.DistributedCache.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.Extensions.DistributedCache.Tests.Extensions;

public class AuthorizationInterceptorBuilderExtensionsTests
{
    [Fact]
    public void UseDistributedCache_ShouldRegisterDistributedCacheDependencies_AndAddInterceptor()
    {
        // Arrange
        var options = Substitute.For<IAuthorizationInterceptorOptions>();

        // Act
        options.UseDistributedCacheInterceptor();

        // Assert
        options.Received(1).UseCustomInterceptor<DistributedCacheAuthorizationInterceptor>(Arg.Any<Func<IServiceCollection, IServiceCollection>?>());
    }
}
