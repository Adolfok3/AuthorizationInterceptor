using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Extensions;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Interceptors;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AuthorizationInterceptor.Tests.Extensions;

public class HttpClientBuilderExtensionsTests
{

    [Fact]
    public void AddAuthorizationInterceptorHandler_ShouldRegisterHandlerAndDefaultOptions_WhenCalledWithGenericType()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = Substitute.For<IHttpClientBuilder>();
        builder.Services.Returns(services);

        // Act
        var result = builder.AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.HttpClientBuilder);
        Assert.NotNull(result);
    }

    [Fact]
    public void AddAuthorizationInterceptorHandler_ShouldApplyCustomOptions_WhenCalledWithOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = Substitute.For<IHttpClientBuilder>();
        builder.Services.Returns(services);

        // Act
        var result = builder.AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(opts =>
        {
            opts.UnauthenticatedPredicate = response => response.StatusCode == System.Net.HttpStatusCode.BadRequest;
            opts.DisableMemoryCache = true;
        });

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.HttpClientBuilder);
        Assert.NotNull(result);
    }
}
