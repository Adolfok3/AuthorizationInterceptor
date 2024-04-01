using AuthorizationInterceptor.Builder;
using AuthorizationInterceptor.Extensions.Memory;
using AuthorizationInterceptor.Interceptors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AuthorizationInterceptor.Tests.Extensions;

public class MemoryAuthorizationInterceptorBuilderExtensionsTests
{
    [Fact]
    public void AddMemoryCache_ShouldRegisterMemoryCacheDependencies_AndAddInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        var httpBuilder = Substitute.For<IHttpClientBuilder>();
        httpBuilder.Services.Returns(services);
        var builder = new AuthorizationInterceptorBuilder(httpBuilder, null, null);

        // Act
        builder.AddMemoryCache();

        // Assert
        Assert.Single(services, service => service.ServiceType == typeof(IMemoryCache));

        var fieldInfo = builder.GetType().GetField("_interceptors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var interceptors = (List<Type>)fieldInfo.GetValue(builder);

        Assert.Contains(typeof(MemoryAuthorizationInterceptor), interceptors);
    }

    [Fact]
    public void AddMemoryCache_WithOptions_ShouldRegisterMemoryCacheDependencies_AndAddInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        var httpBuilder = Substitute.For<IHttpClientBuilder>();
        httpBuilder.Services.Returns(services);
        var builder = new AuthorizationInterceptorBuilder(httpBuilder, null, null);

        // Act
        builder.AddMemoryCache(opt => opt.CompactionPercentage = 2);

        // Assert
        Assert.Single(services, service => service.ServiceType == typeof(IMemoryCache));

        var fieldInfo = builder.GetType().GetField("_interceptors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var interceptors = (List<Type>)fieldInfo.GetValue(builder);

        Assert.Contains(typeof(MemoryAuthorizationInterceptor), interceptors);
    }
}
