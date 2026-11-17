using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Strategies;
using AuthorizationInterceptor.Tests.Utils;
using AuthorizationInterceptor.Utils;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace AuthorizationInterceptor.Tests.Strategies;

public class AuthorizationInterceptorStrategyTests
{
    private readonly IAuthorizationInterceptor _interceptor1;
    private readonly IAuthorizationInterceptor _interceptor2;
    private readonly IAuthorizationInterceptor _interceptor3;
    private AuthorizationInterceptorStrategy _stategy;
    private readonly ILogger _logger;

    public AuthorizationInterceptorStrategyTests()
    {
        _interceptor1 = Substitute.For<IAuthorizationInterceptor>();
        _interceptor2 = Substitute.For<IAuthorizationInterceptor>();
        _interceptor3 = Substitute.For<IAuthorizationInterceptor>();
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(LogLevel.Debug).Returns(true);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger("AuthorizationInterceptorStrategy").Returns(_logger);
        _stategy = new AuthorizationInterceptorStrategy(loggerFactory, [_interceptor1, _interceptor2, _interceptor3], new AuthenticationLock(AuthenticationLockMode.Local, new AuthenticationSingleFlight(), null, null));
    }

    [Fact]
    public async Task GetHeadersAsyncWithCancellationTokenShouldThrowsOperationCanceledException()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var mock = Task.Delay(2000).ContinueWith(_ => MockAuthorizationHeaders.CreateHeaders());
        _interceptor1.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(new ValueTask<AuthorizationHeaders?>(mock));

        //Act
        Func<Task> act = async () => await _stategy.GetHeadersAsync("test", authentication, cancellationToken.Token);

        //Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetHeadersAsyncShouldGetOnlyInAuthenticationHandler()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        authentication.AuthenticateAsync(null, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        headers.Should().BeNull();
        await authentication.Received(1).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsyncWithNoInterceptorsShouldGetFromAuthenticationHandler()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        var expectedHeaders = MockAuthorizationHeaders.CreateHeaders();
        authentication.AuthenticateAsync(null, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(expectedHeaders));

        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger("AuthorizationInterceptorStrategy").Returns(_logger);
        var strategy = new AuthorizationInterceptorStrategy(loggerFactory, [], new AuthenticationLock(AuthenticationLockMode.Local, new AuthenticationSingleFlight(), null, null));

        //Act
        var headers = await strategy.GetHeadersAsync("test_user-1", authentication, CancellationToken.None);

        //Assert
        headers.Should().BeSameAs(expectedHeaders);
        await authentication.Received(1).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsyncWithNoExpiresShouldGetFromFirstInterceptor()
    {
        //Arrange
        var validHeaders = new AuthorizationHeaders(null);
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor1.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(validHeaders));
        _interceptor2.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));
        _interceptor3.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        headers.Should().NotBeNull();
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(0).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsyncWithExpiresAndValidHeadersShouldGetFromFirstInterceptor()
    {
        //Arrange
        var validHeaders = new AuthorizationHeaders(TimeSpan.FromSeconds(10));
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor1.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(validHeaders));
        _interceptor2.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));
        _interceptor3.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        headers.Should().NotBeNull();
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(0).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsyncWithExpiresAndInvalidHeadersShouldGetFromFirstInterceptorAndUpdateInAll()
    {
        //Arrange
        AuthorizationHeaders invalidHeaders = new OAuthHeaders("test", "test", 1, "test", 500);
        var authentication = Substitute.For<IAuthenticationHandler>();
        var mock = Task.Delay(2000).ContinueWith(_ => invalidHeaders);
        _interceptor1.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(new ValueTask<AuthorizationHeaders?>(mock));
        _interceptor2.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));
        _interceptor3.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));
        authentication.AuthenticateAsync(Arg.Is(invalidHeaders), Arg.Any<CancellationToken>()).Returns(MockAuthorizationHeaders.CreateHeaders());

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        headers.Should().NotBeNull();
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(1).AuthenticateAsync(Arg.Is(invalidHeaders), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsyncShouldGetFromSecondInterceptor()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor1.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));
        _interceptor2.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));
        _interceptor3.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        headers.Should().NotBeNull();
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(0).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsyncShouldGetFromThirdInterceptorWithSecondError()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor1.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));
        _interceptor2.GetHeadersAsync("test", Arg.Any<CancellationToken>()).ThrowsAsync(new ArgumentException());
        _interceptor3.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        headers.Should().NotBeNull();
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(0).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateHeadersAsyncWithCancellationTokenShouldThrowsOperationCanceledException()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var mock = Task.Delay(2000).ContinueWith(_ => ValueTask.CompletedTask);
        authentication.AuthenticateAsync(null, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));
        _interceptor1.UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>()).Returns(new ValueTask(mock));
        _interceptor2.UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>()).Returns(new ValueTask(mock));

        //Act
        Func<Task> act = async () => await _stategy.UpdateHeadersAsync("test", null, authentication, cancellationToken.Token);

        //Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task UpdateHeadersAsyncShouldNotUpdateInSecondInterceptors()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor2.UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>()).Throws(new ArgumentException());
        authentication.AuthenticateAsync(null, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));

        //Act
        var act = async () => await _stategy.UpdateHeadersAsync("test", null, authentication, CancellationToken.None);

        //Assert
        (await Record.ExceptionAsync(act)).Should().BeNull();
        await authentication.Received(1).AuthenticateAsync(null, Arg.Any<CancellationToken>());
        await _interceptor1.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
        await _interceptor3.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateHeadersAsyncShouldUpdateInInterceptors()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        authentication.AuthenticateAsync(null, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));

        //Act
        var act = async () => await _stategy.UpdateHeadersAsync("test", null, authentication, CancellationToken.None);

        //Assert
        (await Record.ExceptionAsync(act)).Should().BeNull();
        await authentication.Received(1).AuthenticateAsync(null, Arg.Any<CancellationToken>());
        await _interceptor1.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsyncWithConcurrentCallersShouldAuthenticateOnlyOnce()
    {
        //Arrange
        var cache = new MockCachingAuthorizationInterceptor();
        var authentication = new MockCountingAuthenticationHandler(TimeSpan.FromMilliseconds(200));
        var strategy = CreateStrategy(cache);

        //Act
        var results = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(_ => Task.Run(async () => await strategy.GetHeadersAsync("test", authentication, CancellationToken.None))));

        //Assert
        authentication.Calls.Should().Be(1);
        results.Should().AllSatisfy(headers => headers.Should().NotBeNull());
    }

    [Fact]
    public async Task GetHeadersAsyncWithConcurrentCallersAndDifferentCacheKeySuffixesShouldAuthenticateOncePerSuffix()
    {
        //Arrange
        var cache = new MockCachingAuthorizationInterceptor();
        var authentication = new MockCountingAuthenticationHandler(TimeSpan.FromMilliseconds(200));
        var strategy = CreateStrategy(cache);

        //Act
        var results = await Task.WhenAll(Enumerable.Range(0, 24)
            .Select(index => Task.Run(async () => await strategy.GetHeadersAsync($"test_tenant-{index % 4}", authentication, CancellationToken.None))));

        //Assert
        authentication.Calls.Should().Be(4);
        results.Should().AllSatisfy(headers => headers.Should().NotBeNull());
    }

    [Fact]
    public async Task UpdateHeadersAsyncWithConcurrentCallersShouldAuthenticateOnlyOnce()
    {
        //Arrange
        var expiredHeaders = MockAuthorizationHeaders.CreateHeaders();
        var cache = new MockCachingAuthorizationInterceptor();
        cache.Seed("test", expiredHeaders);

        var authentication = new MockCountingAuthenticationHandler(TimeSpan.FromMilliseconds(200));
        var strategy = CreateStrategy(cache);

        //Act
        var results = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(_ => Task.Run(async () => await strategy.UpdateHeadersAsync("test", expiredHeaders, authentication, CancellationToken.None))));

        //Assert
        authentication.Calls.Should().Be(1);
        results.Should().AllSatisfy(headers =>
        {
            headers.Should().NotBeNull();
            headers.Should().NotBeSameAs(expiredHeaders);
        });
    }

    [Fact]
    public async Task UpdateHeadersAsyncWhenJoinedFlightIsNotNewerThanRejectedHeadersShouldReauthenticate()
    {
        //Arrange
        // The shared authentication resolves to headers that predate the ones the target API rejected,
        // which is what happens when the flight started before this caller got its 401. Callers that
        // joined it must not settle for that generation.
        var staleResult = MockAuthorizationHeaders.CreateHeaders();
        await Task.Delay(20);
        var expiredHeaders = MockAuthorizationHeaders.CreateHeaders();

        var authentication = new MockCountingAuthenticationHandler(TimeSpan.FromMilliseconds(200), () => staleResult);
        var strategy = CreateStrategy(new MockCachingAuthorizationInterceptor());

        //Act
        var results = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => Task.Run(async () => await strategy.UpdateHeadersAsync("test", expiredHeaders, authentication, CancellationToken.None))));

        //Assert
        // Without the guard every caller would have settled for the single shared flight.
        (authentication.Calls > 1).Should().BeTrue($"expected the guard to force a re-authentication, got {authentication.Calls} call(s)");
        results.Should().AllSatisfy(headers => headers.Should().BeSameAs(staleResult));
    }

    [Fact]
    public async Task UpdateHeadersAsyncWithConcurrentCallersAndNoHeadersGeneratedShouldAuthenticateOnlyOnce()
    {
        //Arrange
        // A handler returning null is a terminal answer, not a stale generation, so the callers that
        // joined the flight must accept it instead of each retrying on its own.
        var authentication = new MockCountingAuthenticationHandler(TimeSpan.FromMilliseconds(200), () => null);
        var strategy = CreateStrategy(new MockCachingAuthorizationInterceptor());

        //Act
        var results = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(_ => Task.Run(async () => await strategy.UpdateHeadersAsync("test", MockAuthorizationHeaders.CreateHeaders(), authentication, CancellationToken.None))));

        //Assert
        authentication.Calls.Should().Be(1);
        results.Should().AllSatisfy(headers => headers.Should().BeNull());
    }

    private static AuthorizationInterceptorStrategy CreateStrategy(params IAuthorizationInterceptor[] interceptors)
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger("AuthorizationInterceptorStrategy").Returns(Substitute.For<ILogger>());

        return new AuthorizationInterceptorStrategy(loggerFactory, interceptors, new AuthenticationLock(AuthenticationLockMode.Local, new AuthenticationSingleFlight(), null, null));
    }
}
