using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Utils;

namespace AuthorizationInterceptor.Tests.Utils;

public class AuthenticationLockTests
{
    private static Func<CancellationToken, Task<AuthorizationHeaders?>> Result(AuthorizationHeaders? headers, Action? onCall = null)
        => _ =>
        {
            onCall?.Invoke();
            return Task.FromResult(headers);
        };

    [Theory]
    [InlineData(AuthenticationLockMode.Distributed)]
    [InlineData(AuthenticationLockMode.Local | AuthenticationLockMode.Distributed)]
    public void Constructor_WithDistributedMode_AndNoProvider_ShouldThrow(AuthenticationLockMode mode)
    {
        //Act
        var act = () => new AuthenticationLock(mode, new AuthenticationSingleFlight(), null, null);

        //Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("IDistributedLockProvider", exception.Message);
    }

    [Theory]
    [InlineData(AuthenticationLockMode.None)]
    [InlineData(AuthenticationLockMode.Local)]
    public void Constructor_WithoutDistributedMode_AndNoProvider_ShouldNotThrow(AuthenticationLockMode mode)
    {
        //Act
        var act = () => new AuthenticationLock(mode, new AuthenticationSingleFlight(), null, null);

        //Assert
        Assert.Null(Record.Exception(act));
    }

    [Fact]
    public void Constructor_WithDistributedMode_AndProvider_ShouldNotThrow()
    {
        //Arrange
        var mockLock = new MockDistributedLock();

        //Act
        var act = () => new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);

        //Assert
        Assert.Null(Record.Exception(act));
    }

    [Fact]
    public async Task RunAsync_WithNoneMode_ShouldAuthenticateWithoutLockingOrRevalidating()
    {
        //Arrange
        var authenticated = MockAuthorizationHeaders.CreateHeaders();
        var authCalls = 0;
        var revalidateCalls = 0;
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.None, new AuthenticationSingleFlight(), null, null);

        //Act
        var result = await authenticationLock.RunAsync(
            "test",
            Result(authenticated, () => authCalls++),
            Result(null, () => revalidateCalls++),
            CancellationToken.None);

        //Assert
        Assert.Same(authenticated, result.Headers);
        Assert.False(result.Joined);
        Assert.Equal(1, authCalls);
        Assert.Equal(0, revalidateCalls);
    }

    [Fact]
    public async Task RunAsync_WithLocalMode_ShouldAuthenticateWithoutRevalidating()
    {
        //Arrange
        var authenticated = MockAuthorizationHeaders.CreateHeaders();
        var revalidateCalls = 0;
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Local, new AuthenticationSingleFlight(), null, null);

        //Act
        var result = await authenticationLock.RunAsync(
            "test",
            Result(authenticated),
            Result(null, () => revalidateCalls++),
            CancellationToken.None);

        //Assert
        Assert.Same(authenticated, result.Headers);
        Assert.Equal(0, revalidateCalls);
    }

    [Fact]
    public async Task RunAsync_WithLocalMode_AndConcurrentCallers_ShouldAuthenticateOnlyOnce()
    {
        //Arrange
        var authCalls = 0;
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Local, new AuthenticationSingleFlight(), null, null);
        Func<CancellationToken, Task<AuthorizationHeaders?>> authenticate = async _ =>
        {
            Interlocked.Increment(ref authCalls);
            await Task.Delay(200);
            return MockAuthorizationHeaders.CreateHeaders();
        };

        //Act
        var results = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(_ => Task.Run(async () => await authenticationLock.RunAsync("test", authenticate, Result(null), CancellationToken.None))));

        //Assert
        Assert.Equal(1, authCalls);
        Assert.All(results, result => Assert.NotNull(result.Headers));
        Assert.Contains(results, result => result.Joined);
    }

    [Fact]
    public async Task RunAsync_WithDistributedMode_ShouldAcquireLock_Revalidate_AndAuthenticate()
    {
        //Arrange
        var authenticated = MockAuthorizationHeaders.CreateHeaders();
        var authCalls = 0;
        var revalidateCalls = 0;
        var mockLock = new MockDistributedLock();
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);

        //Act
        var result = await authenticationLock.RunAsync(
            "test",
            Result(authenticated, () => authCalls++),
            Result(null, () => revalidateCalls++),
            CancellationToken.None);

        //Assert
        Assert.Same(authenticated, result.Headers);
        Assert.False(result.Joined);
        Assert.Equal(1, authCalls);
        Assert.Equal(1, revalidateCalls);
        Assert.Equal(1, mockLock.AcquireCount);
        Assert.Equal("authorizationinterceptor:test", mockLock.LastLockName);
        _ = mockLock.Handle.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task RunAsync_WithDistributedMode_WhenRevalidateReturnsHeaders_ShouldSkipAuthentication()
    {
        //Arrange
        var revalidated = MockAuthorizationHeaders.CreateHeaders();
        var authCalls = 0;
        var mockLock = new MockDistributedLock();
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);

        //Act
        var result = await authenticationLock.RunAsync(
            "test",
            Result(MockAuthorizationHeaders.CreateHeaders(), () => authCalls++),
            Result(revalidated),
            CancellationToken.None);

        //Assert
        Assert.Same(revalidated, result.Headers);
        Assert.Equal(0, authCalls);
        Assert.Equal(1, mockLock.AcquireCount);
    }

    [Fact]
    public async Task RunAsync_WithLocalAndDistributedMode_ShouldAcquireLock_Revalidate_AndAuthenticate()
    {
        //Arrange
        var authenticated = MockAuthorizationHeaders.CreateHeaders();
        var authCalls = 0;
        var revalidateCalls = 0;
        var mockLock = new MockDistributedLock();
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Local | AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);

        //Act
        var result = await authenticationLock.RunAsync(
            "test",
            Result(authenticated, () => authCalls++),
            Result(null, () => revalidateCalls++),
            CancellationToken.None);

        //Assert
        Assert.Same(authenticated, result.Headers);
        Assert.Equal(1, authCalls);
        Assert.Equal(1, revalidateCalls);
        Assert.Equal(1, mockLock.AcquireCount);
    }

    [Fact]
    public async Task RunAsync_WithDistributedMode_ShouldAcquireLockUsingConfiguredTimeout()
    {
        //Arrange
        var timeout = TimeSpan.FromSeconds(5);
        var mockLock = new MockDistributedLock();
        var authenticationLock = new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, timeout);

        //Act
        await authenticationLock.RunAsync("test", Result(MockAuthorizationHeaders.CreateHeaders()), Result(null), CancellationToken.None);

        //Assert
        _ = mockLock.Lock.Received(1).AcquireAsync(timeout, Arg.Any<CancellationToken>());
    }
}
