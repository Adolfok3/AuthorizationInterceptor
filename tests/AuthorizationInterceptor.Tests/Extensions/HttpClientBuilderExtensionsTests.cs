using AuthorizationInterceptor.Extensions;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Options;
using AuthorizationInterceptor.Tests.Utils;
using Medallion.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.Tests.Extensions;

public class HttpClientBuilderExtensionsTests
{

    [Fact]
    public void AddClientCredentialsAuthorizationInterceptorHandlerWithValidOptionsShouldBuildClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("Test")
            .AddClientCredentialsAuthorizationInterceptorHandler(options =>
            {
                options.TokenEndpoint = new Uri("https://identity.example.com/token");
                options.ClientId = "client";
                options.ClientSecret = "secret";
            });

        var provider = services.BuildServiceProvider();

        // Act
        var action = () => provider.GetRequiredService<IHttpClientFactory>().CreateClient("Test");

        // Assert
        Record.Exception(action).Should().BeNull();
    }

    [Fact]
    public void AddClientCredentialsAuthorizationInterceptorHandlerWithInvalidOptionsShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddHttpClient("Test")
            .AddClientCredentialsAuthorizationInterceptorHandler(_ => { });

        // Assert
        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithoutOptionsShouldExecuteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = Substitute.For<IHttpClientBuilder>();
        builder.Services.Returns(services);

        // Act
        var act = () => builder.AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>();

        // Assert
        Record.Exception(act).Should().BeNull();
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithOptionsShouldExecuteSuccessfully()
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
        Record.Exception(act).Should().BeNull();
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithOptionsShouldBuildServiceProviderSucessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(options => options.UseCustomInterceptor<MockAuthorizationInterceptor>(func => func.AddSingleton<MockAuthorizationInterceptor>()));

        // Act
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var act = () => httpClientFactory.CreateClient("Test");

        // Assert
        Record.Exception(act).Should().BeNull();
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithNullAuthHandlerShouldThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        IHttpClientBuilder act() => services.AddHttpClient("Test").AddAuthorizationInterceptorHandler(authHandlerImpl: default!);

        // Assert
        ((Func<IHttpClientBuilder>)act).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithOptionsAndAuthHandlerShouldBuildServiceProviderSucessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler((provider) => ActivatorUtilities.CreateInstance<MockAuthorizationInterceptorAuthenticationHandler>(provider), options => options.UseCustomInterceptor<MockAuthorizationInterceptor>(func => func.AddSingleton<MockAuthorizationInterceptor>()));

        // Act
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var act = () => httpClientFactory.CreateClient("Test");

        // Assert
        Record.Exception(act).Should().BeNull();
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithAuthHandlerShouldRegisterHttpContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler((provider) => ActivatorUtilities.CreateInstance<MockAuthorizationInterceptorAuthenticationHandler>(provider), options => options.CacheKeyBuilder = accessor => accessor.HttpContext?.Request.Headers["x-mycustom-header"].ToString());

        // Assert
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithInterceptorDependenciesShouldRegisterThemAtRegistrationTime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(options => options.UseCustomInterceptor<MockDependentAuthorizationInterceptor>(func => func.AddSingleton<MockInterceptorDependency>()));

        // Assert
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(MockInterceptorDependency));
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithDistributedLockModeAndNoProviderShouldThrowWhenCreatingClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(options => options.LockMode = AuthenticationLockMode.Distributed);

        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();

        // Act
        var act = () => httpClientFactory.CreateClient("Test");

        // Assert
        var exception = act.Should().ThrowExactly<InvalidOperationException>().WithMessage("IDistributedLockProvider");
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithDistributedLockModeAndProviderRegisteredShouldBuildServiceProviderSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDistributedLockProvider>());
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(options =>
            {
                options.LockMode = AuthenticationLockMode.Local | AuthenticationLockMode.Distributed;
                options.DistributedLockTimeout = TimeSpan.FromSeconds(10);
            });

        // Act
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var act = () => httpClientFactory.CreateClient("Test");

        // Assert
        Record.Exception(act).Should().BeNull();
    }

    [Fact]
    public void AddAuthorizationInterceptorHandlerWithInterceptorDependenciesShouldCreateClientAfterServiceCollectionIsReadOnly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient("Test")
            .AddAuthorizationInterceptorHandler<MockAuthorizationInterceptorAuthenticationHandler>(options => options.UseCustomInterceptor<MockDependentAuthorizationInterceptor>(func => func.AddSingleton<MockInterceptorDependency>()));

        var provider = services.BuildServiceProvider();

        // The generic host seals the collection once the provider is built, so anything the interceptors
        // need must already be registered by the time the handler chain is created.
        services.MakeReadOnly();

        // Act
        var act = () => provider.GetRequiredService<IHttpClientFactory>().CreateClient("Test");

        // Assert
        Record.Exception(act).Should().BeNull();
    }
}
