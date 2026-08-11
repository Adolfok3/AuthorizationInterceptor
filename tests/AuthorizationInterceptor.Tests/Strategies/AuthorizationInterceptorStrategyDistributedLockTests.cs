using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Strategies;
using AuthorizationInterceptor.Tests.Utils;
using AuthorizationInterceptor.Utils;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Tests.Strategies;

public class AuthorizationInterceptorStrategyDistributedLockTests
{
    private static AuthorizationInterceptorStrategy CreateStrategy(AuthenticationLock authenticationLock, params IAuthorizationInterceptor[] interceptors)
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger("AuthorizationInterceptorStrategy").Returns(Substitute.For<ILogger>());

        return new AuthorizationInterceptorStrategy(loggerFactory, interceptors, authenticationLock);
    }

    [Fact]
    public async Task GetHeadersAsync_WithDistributedLock_WhenConcurrentInstanceRefreshedCache_ShouldSkipAuthentication()
    {
        //Arrange
        var cache = new MockCachingAuthorizationInterceptor();
        var refreshedByOtherInstance = MockAuthorizationHeaders.CreateHeaders();
        // Simulate another instance writing fresh headers to the shared cache while this caller waited for the lock.
        var mockLock = new MockDistributedLock(onAcquire: () => cache.Seed("test", refreshedByOtherInstance));
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);
        var authentication = new MockCountingAuthenticationHandler(TimeSpan.Zero);
        var strategy = CreateStrategy(authenticationLock, cache);

        //Act
        var headers = await strategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        Assert.Same(refreshedByOtherInstance, headers);
        Assert.Equal(0, authentication.Calls);
        Assert.Equal(1, mockLock.AcquireCount);
        Assert.Equal("authorizationinterceptor:test", mockLock.LastLockName);
    }

    [Fact]
    public async Task GetHeadersAsync_WithDistributedLock_WhenCacheStillEmptyAfterLock_ShouldAuthenticate()
    {
        //Arrange
        var cache = new MockCachingAuthorizationInterceptor();
        var mockLock = new MockDistributedLock();
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);
        var authentication = new MockCountingAuthenticationHandler(TimeSpan.Zero);
        var strategy = CreateStrategy(authenticationLock, cache);

        //Act
        var headers = await strategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        Assert.NotNull(headers);
        Assert.Equal(1, authentication.Calls);
        Assert.Equal(1, mockLock.AcquireCount);
    }

    [Fact]
    public async Task UpdateHeadersAsync_WithDistributedLock_WhenConcurrentInstanceRefreshedCache_ShouldSkipAuthentication()
    {
        //Arrange
        var expiredHeaders = MockAuthorizationHeaders.CreateHeaders();
        await Task.Delay(20);
        var refreshedByOtherInstance = MockAuthorizationHeaders.CreateHeaders();

        var cache = new MockCachingAuthorizationInterceptor();
        var mockLock = new MockDistributedLock(onAcquire: () => cache.Seed("test", refreshedByOtherInstance));
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);
        var authentication = new MockCountingAuthenticationHandler(TimeSpan.Zero);
        var strategy = CreateStrategy(authenticationLock, cache);

        //Act
        var headers = await strategy.UpdateHeadersAsync("test", expiredHeaders, authentication, CancellationToken.None);

        //Assert
        Assert.Same(refreshedByOtherInstance, headers);
        Assert.Equal(0, authentication.Calls);
    }

    [Fact]
    public async Task UpdateHeadersAsync_WithDistributedLock_WhenCacheHeadersAreNotNewerThanExpired_ShouldAuthenticate()
    {
        //Arrange
        // Another instance's cache entry predates the headers rejected by the target API, so the double-check
        // must not adopt it; a fresh authentication is still required.
        var stale = MockAuthorizationHeaders.CreateHeaders();
        await Task.Delay(20);
        var expiredHeaders = MockAuthorizationHeaders.CreateHeaders();

        var cache = new MockCachingAuthorizationInterceptor();
        var mockLock = new MockDistributedLock(onAcquire: () => cache.Seed("test", stale));
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);
        var authentication = new MockCountingAuthenticationHandler(TimeSpan.Zero);
        var strategy = CreateStrategy(authenticationLock, cache);

        //Act
        var headers = await strategy.UpdateHeadersAsync("test", expiredHeaders, authentication, CancellationToken.None);

        //Assert
        Assert.NotNull(headers);
        Assert.Equal(1, authentication.Calls);
    }

    [Fact]
    public async Task GetHeadersAsync_WithNoInterceptors_ShouldBypassDistributedLock()
    {
        //Arrange
        var mockLock = new MockDistributedLock();
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);
        var authentication = Substitute.For<IAuthenticationHandler>();
        authentication.AuthenticateAsync(null, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));
        var strategy = CreateStrategy(authenticationLock);

        //Act
        var headers = await strategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        Assert.NotNull(headers);
        Assert.Equal(0, mockLock.AcquireCount);
        await authentication.Received(1).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }
}
