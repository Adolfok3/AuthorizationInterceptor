using AuthorizationInterceptor.Builder;
using AuthorizationInterceptor.Interceptors;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AuthorizationInterceptor.Tests.Builder;

public class AuthorizationInterceptorBuilderTests
{
    [Fact]
    public void BuildAuthorizationInterceptor_ShouldAddDefaultInterceptor_AndBuildeCorrectly()
    {
        // Arrange
        var httpClientBuilder = Substitute.For<IHttpClientBuilder>();
        var builder = new AuthorizationInterceptorBuilder(httpClientBuilder, Substitute.For<Type>(), new Options.AuthorizationInterceptorOptions());

        // Act
        var exception = Record.Exception(builder.BuildAuthorizationInterceptor);

        // Assert
        Assert.Null(exception);
        var fieldInfo = builder.GetType().GetField("_interceptors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var interceptors = (List<Type>)fieldInfo.GetValue(builder);

        Assert.Contains(typeof(DefaultAuthorizationInterceptor), interceptors);
    }

    [Fact]
    public void BuildAuthorizationInterceptor_WithCustomInterceptors_ShouldBuildCorrectly()
    {
        // Arrange
        var httpClientBuilder = Substitute.For<IHttpClientBuilder>();
        var builder = new AuthorizationInterceptorBuilder(httpClientBuilder, Substitute.For<Type>(), new Options.AuthorizationInterceptorOptions());

        // Act
        builder.AddCustom<MockAuthorizationInterceptor>();
        builder.AddCustom<Mock2AuthorizationInterceptor>();
        builder.AddCustom<Mock3AuthorizationInterceptor>();
        var exception = Record.Exception(builder.BuildAuthorizationInterceptor);

        // Assert
        Assert.Null(exception);
        var fieldInfo = builder.GetType().GetField("_interceptors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var interceptors = (List<Type>)fieldInfo.GetValue(builder);

        Assert.Contains(typeof(MockAuthorizationInterceptor), interceptors);
        Assert.Contains(typeof(Mock2AuthorizationInterceptor), interceptors);
        Assert.Contains(typeof(Mock3AuthorizationInterceptor), interceptors);
    }
}
