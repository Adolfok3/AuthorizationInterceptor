using AuthorizationInterceptor.Extensions;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.AspNetCore.Http;
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
                options.UseCustomInterceptor<MockAuthorizationInterceptor>(func => func.AddSingleton<MockAuthorizationInterceptor>());
            });

        // Act
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var act = () => httpClientFactory.CreateClient("Test");

        // Assert
        Assert.Null(Record.Exception(act));
    }

    [Fact]
    public void AddAuthorizationInterceptorHandler_WithNullAuthHandler_ShouldThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        IHttpClientBuilder act() => services.AddHttpClient("Test").AddAuthorizationInterceptorHandler(authHandlerImpl: null);

        // Assert
        Assert.Throws<ArgumentNullException>((Func<IHttpClientBuilder>)act);
    }

    [Fact]
    public void AddAuthorizationInterceptorHandler_WithOptionsAndAuthHandler_ShouldBuildServiceProviderSucessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler((provider) => ActivatorUtilities.CreateInstance<MockAuthorizationInterceptorAuthenticationHandler>(provider), options =>
            {
                options.UseCustomInterceptor<MockAuthorizationInterceptor>(func => func.AddSingleton<MockAuthorizationInterceptor>());
            });

        // Act
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var act = () => httpClientFactory.CreateClient("Test");

        // Assert
        Assert.Null(Record.Exception(act));
    }

    [Fact]
    public void AddAuthorizationInterceptorHandler_WithAuthHandler_ShouldRegisterHttpContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler((provider) => ActivatorUtilities.CreateInstance<MockAuthorizationInterceptorAuthenticationHandler>(provider), options =>
            {
                options.CacheKeyBuilder = accessor => accessor.HttpContext?.Request.Headers["x-mycustom-header"].ToString();
            });

        // Assert
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void AddAuthorizationInterceptorHandler_WithInterceptorDependencies_ShouldRegisterThemAtRegistrationTime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(options =>
            {
                options.UseCustomInterceptor<MockDependentAuthorizationInterceptor>(func => func.AddSingleton<MockInterceptorDependency>());
            });

        // Assert
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(MockInterceptorDependency));
    }

    [Fact]
    public void AddAuthorizationInterceptorHandler_WithInterceptorDependencies_ShouldCreateClientAfterServiceCollectionIsReadOnly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(options =>
            {
                options.UseCustomInterceptor<MockDependentAuthorizationInterceptor>(func => func.AddSingleton<MockInterceptorDependency>());
            });

        var provider = services.BuildServiceProvider();

        // The generic host seals the collection once the provider is built, so anything the interceptors
        // need must already be registered by the time the handler chain is created.
        services.MakeReadOnly();

        // Act
        var act = () => provider.GetRequiredService<IHttpClientFactory>().CreateClient("Test");

        // Assert
        Assert.Null(Record.Exception(act));
    }
}
