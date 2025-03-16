using AuthorizationInterceptor.Extensions;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.Tests.Extensions;

public class HttpClientBuilderExtensionsTests
{

    [Fact]
    public void AddAuthorizationInterceptorHandler_WithoutOptions_ShouldExecuteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = Substitute.For<IHttpClientBuilder>();
        builder.Services.Returns(services);

        // Act
        var act = () => builder.AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>();

        // Assert
        Assert.Null(Record.Exception(act));
    }

    [Fact]
    public void AddAuthorizationInterceptorHandler_WithOptions_ShouldExecuteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = Substitute.For<IHttpClientBuilder>();
        builder.Services.Returns(services);

        // Act
        var act = () => builder.AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(opts =>
        {
            opts.UnauthenticatedPredicate = response => response.StatusCode == System.Net.HttpStatusCode.BadRequest;
            opts.UseCustomInterceptor<MockAuthorizationInterceptor>();
        });

        // Assert
        Assert.Null(Record.Exception(act));
    }



    [Fact]
    public void AddAuthorizationInterceptorHandler_WithOptions_ShouldBuildServiceProviderSucessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(options =>
            {
                options.UseCustomInterceptor<MockAuthorizationInterceptor>();
            });

        // Act
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var act = () => httpClientFactory.CreateClient("Test");

        // Assert
        Assert.Null(Record.Exception(act));
    }
}
