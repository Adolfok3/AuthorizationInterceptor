using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.MemoryCache.Extensions;
using AuthorizationInterceptor.Extensions.MemoryCache.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.Extensions.MemoryCache.Tests.Extensions;

public class AuthorizationInterceptorOptionsExtensionsTests
{
    [Fact]
    public void UseMemoryCacheInterceptor_WithoutOptions_ShouldAddSuccessfully()
    {
        //Arrange
        var options = Substitute.For<IAuthorizationInterceptorOptions>();

        //Act
        var act = () => options.UseMemoryCacheInterceptor();

        //Assert
        act.Should().NotThrow();
        options.Received(1).UseCustomInterceptor<MemoryCacheInterceptor>(Arg.Any<Func<IServiceCollection, IServiceCollection>?>());
    }
}
