using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Utils;
using FluentAssertions;

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
    public void ConstructorWithDistributedModeAndNoProviderShouldThrow(AuthenticationLockMode mode)
    {
        //Act
        var act = () => new AuthenticationLock(mode, new AuthenticationSingleFlight(), null, null);

        //Assert
        var exception = act.Should().ThrowExactly<InvalidOperationException>();
        exception.Message.Should().Contain("IDistributedLockProvider");
    }

    [Theory]
    [InlineData(AuthenticationLockMode.None)]
    [InlineData(AuthenticationLockMode.Local)]
    public void ConstructorWithoutDistributedModeAndNoProviderShouldNotThrow(AuthenticationLockMode mode)
    {
        //Act
        var act = () => new AuthenticationLock(mode, new AuthenticationSingleFlight(), null, null);

        //Assert
        Record.Exception(act).Should().BeNull();
    }

    [Fact]
    public void ConstructorWithDistributedModeAndProviderShouldNotThrow()
    {
        //Arrange
        var mockLock = new MockDistributedLock();

        //Act
        var act = () => new AuthenticationLock(AuthenticationLockMode.Distributed, new AuthenticationSingleFlight(), mockLock.Provider, null);

        //Assert
        Record.Exception(act).Should().BeNull();
    }

    [Fact]
    public async Task RunAsyncWithNoneModeShouldAuthenticateWithoutLockingOrRevalidating()
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
        result.Headers.Should().BeSameAs(authenticated);
        result.Joined.Should().BeFalse();
        authCalls.Should().Be(1);
        revalidateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RunAsyncWithLocalModeShouldAuthenticateWithoutRevalidating()
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
        result.Headers.Should().BeSameAs(authenticated);
        revalidateCalls.Should().Be(0);
    }

    [Fact]
    public async Task RunAsyncWithLocalModeAndConcurrentCallersShouldAuthenticateOnlyOnce()
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
        authCalls.Should().Be(1);
        Assert.All(results, result => result.Headers.Should().NotBeNull());
        (result => result.Joined).Should().Contain(results);
    }

    [Fact]
    public async Task RunAsyncWithDistributedModeShouldAcquireLockRevalidateAndAuthenticate()
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
        result.Headers.Should().BeSameAs(authenticated);
        result.Joined.Should().BeFalse();
        authCalls.Should().Be(1);
        revalidateCalls.Should().Be(1);
        mockLock.AcquireCount.Should().Be(1);
        mockLock.LastLockName.Should().Be("authorizationinterceptor:test");
        _ = mockLock.Handle.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task RunAsyncWithDistributedModeWhenRevalidateReturnsHeadersShouldSkipAuthentication()
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
        result.Headers.Should().BeSameAs(revalidated);
        authCalls.Should().Be(0);
        mockLock.AcquireCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsyncWithLocalAndDistributedModeShouldAcquireLockRevalidateAndAuthenticate()
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
        result.Headers.Should().BeSameAs(authenticated);
        authCalls.Should().Be(1);
        revalidateCalls.Should().Be(1);
        mockLock.AcquireCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsyncWithDistributedModeShouldAcquireLockUsingConfiguredTimeout()
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
