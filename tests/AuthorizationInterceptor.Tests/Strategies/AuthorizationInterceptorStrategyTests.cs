using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Strategies;
using AuthorizationInterceptor.Tests.Utils;
using AuthorizationInterceptor.Utils;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace AuthorizationInterceptor.Tests.Strategies;

public class AuthorizationInterceptorStrategyTests
{
    private readonly IAuthorizationInterceptor _interceptor1;
    private readonly IAuthorizationInterceptor _interceptor2;
    private readonly IAuthorizationInterceptor _interceptor3;
    private IAuthorizationInterceptorStrategy _stategy;
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
    public async Task GetHeadersAsync_WithCancellationToken_ShouldThrowsOperationCanceledException()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var mock = Task.Delay(2000).ContinueWith(_ => MockAuthorizationHeaders.CreateHeaders());
        _interceptor1.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(new ValueTask<AuthorizationHeaders?>(mock));

        //Act
        Func<Task> act = async () => await _stategy.GetHeadersAsync("test", authentication, cancellationToken.Token);

        //Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetOnlyInAuthenticationHandler()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        authentication.AuthenticateAsync(null, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        Assert.Null(headers);
        await authentication.Received(1).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsync_WithNoInterceptors_ShouldGetFromAuthenticationHandler()
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
        Assert.Same(expectedHeaders, headers);
        await authentication.Received(1).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsync_WithNoExpires_ShouldGetFromFirstInterceptor()
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
        Assert.NotNull(headers);
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(0).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsync_WithExpires_AndValidHeaders_ShouldGetFromFirstInterceptor()
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
        Assert.NotNull(headers);
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(0).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsync_WithExpires_AndInvalidHeaders_ShouldGetFromFirstInterceptor_AndUpdateInAll()
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
        Assert.NotNull(headers);
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(1).AuthenticateAsync(Arg.Is(invalidHeaders), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromSecondInterceptor()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor1.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));
        _interceptor2.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));
        _interceptor3.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        Assert.NotNull(headers);
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(0).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromThirdInterceptor_WithSecondError()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor1.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));
        _interceptor2.GetHeadersAsync("test", Arg.Any<CancellationToken>()).ThrowsAsync(new ArgumentException());
        _interceptor3.GetHeadersAsync("test", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication, CancellationToken.None);

        //Assert
        Assert.NotNull(headers);
        await _interceptor1.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor3.Received(1).GetHeadersAsync("test", Arg.Any<CancellationToken>());
        await _interceptor1.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers, Arg.Any<CancellationToken>());
        await authentication.Received(0).AuthenticateAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateHeadersAsync_WithCancellationToken_ShouldThrowsOperationCanceledException()
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
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task UpdateHeadersAsync_ShouldNotUpdateInSecondInterceptors()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor2.UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>()).Throws(new ArgumentException());
        authentication.AuthenticateAsync(null, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));

        //Act
        var act = async () => await _stategy.UpdateHeadersAsync("test", null, authentication, CancellationToken.None);

        //Assert
        Assert.Null(await Record.ExceptionAsync(act));
        await authentication.Received(1).AuthenticateAsync(null, Arg.Any<CancellationToken>());
        await _interceptor1.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
        await _interceptor2.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
        await _interceptor3.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateHeadersAsync_ShouldUpdateInInterceptors()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        authentication.AuthenticateAsync(null, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AuthorizationHeaders?>(null));

        //Act
        var act = async () => await _stategy.UpdateHeadersAsync("test", null, authentication, CancellationToken.None);

        //Assert
        Assert.Null(await Record.ExceptionAsync(act));
        await authentication.Received(1).AuthenticateAsync(null, Arg.Any<CancellationToken>());
        await _interceptor1.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
        await _interceptor2.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsync_WithConcurrentCallers_ShouldAuthenticateOnlyOnce()
    {
        //Arrange
        var cache = new MockCachingAuthorizationInterceptor();
        var authentication = new MockCountingAuthenticationHandler(TimeSpan.FromMilliseconds(200));
        var strategy = CreateStrategy(cache);

        //Act
        var results = await Task.WhenAll(Enumerable.Range(0, 25)
            .Select(_ => Task.Run(async () => await strategy.GetHeadersAsync("test", authentication, CancellationToken.None))));

        //Assert
        Assert.Equal(1, authentication.Calls);
        Assert.All(results, headers => Assert.NotNull(headers));
    }

    [Fact]
    public async Task GetHeadersAsync_WithConcurrentCallers_AndDifferentCacheKeySuffixes_ShouldAuthenticateOncePerSuffix()
    {
        //Arrange
        var cache = new MockCachingAuthorizationInterceptor();
        var authentication = new MockCountingAuthenticationHandler(TimeSpan.FromMilliseconds(200));
        var strategy = CreateStrategy(cache);

        //Act
        var results = await Task.WhenAll(Enumerable.Range(0, 24)
            .Select(index => Task.Run(async () => await strategy.GetHeadersAsync($"test_tenant-{index % 4}", authentication, CancellationToken.None))));

        //Assert
        Assert.Equal(4, authentication.Calls);
        Assert.All(results, headers => Assert.NotNull(headers));
    }

    [Fact]
    public async Task UpdateHeadersAsync_WithConcurrentCallers_ShouldAuthenticateOnlyOnce()
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
        Assert.Equal(1, authentication.Calls);
        Assert.All(results, headers => Assert.NotNull(headers));
        Assert.All(results, headers => Assert.NotSame(expiredHeaders, headers));
    }

    [Fact]
    public async Task UpdateHeadersAsync_WhenJoinedFlightIsNotNewerThanRejectedHeaders_ShouldReauthenticate()
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
        Assert.True(authentication.Calls > 1, $"expected the guard to force a re-authentication, got {authentication.Calls} call(s)");
        Assert.All(results, headers => Assert.Same(staleResult, headers));
    }

    [Fact]
    public async Task UpdateHeadersAsync_WithConcurrentCallers_AndNoHeadersGenerated_ShouldAuthenticateOnlyOnce()
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
        Assert.Equal(1, authentication.Calls);
        Assert.All(results, Assert.Null);
    }

    private static AuthorizationInterceptorStrategy CreateStrategy(params IAuthorizationInterceptor[] interceptors)
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger("AuthorizationInterceptorStrategy").Returns(Substitute.For<ILogger>());

        return new AuthorizationInterceptorStrategy(loggerFactory, interceptors, new AuthenticationLock(AuthenticationLockMode.Local, new AuthenticationSingleFlight(), null, null));
    }
}
