using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Strategies;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AuthorizationInterceptor.Tests.Strategies;

public class AuthorizationInterceptorStrategyTests
{
    private readonly IAuthenticationHandler _authentication;
    private readonly ILoggerFactory _factory;
    private IAuthorizationInterceptorStrategy _stategy;

    public AuthorizationInterceptorStrategyTests()
    {
        _factory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Debug).Returns(true);
        _factory.CreateLogger("AuthorizationInterceptorStrategy").Returns(logger);
        _authentication = Substitute.For<IAuthenticationHandler>();
        _stategy = new AuthorizationInterceptorStrategy(_authentication, _factory, []);
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetOnlyInAuthenticationHandler()
    {
        //Arrange
        _authentication.AuthenticateAsync().Returns(Task.FromResult<AuthorizationHeaders?>(null));

        //Act
        var headers = await _stategy.GetHeadersAsync();

        //Assert
        Assert.Null(headers);
        await _authentication.Received(1).AuthenticateAsync();
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromSecondInterceptor()
    {
        //Arrange
        var interceptor1 = Substitute.For<IAuthorizationInterceptor>();
        var interceptor2 = Substitute.For<IAuthorizationInterceptor>();
        var interceptor3 = Substitute.For<IAuthorizationInterceptor>();
        interceptor1.GetHeadersAsync().Returns(Task.FromResult<AuthorizationHeaders?>(null));
        interceptor2.GetHeadersAsync().Returns(Task.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));
        interceptor3.GetHeadersAsync().Returns(Task.FromResult<AuthorizationHeaders?>(null));
        IAuthorizationInterceptor[] interceptors = [interceptor1, interceptor2, interceptor3];

        _stategy = new AuthorizationInterceptorStrategy(_authentication, _factory, interceptors);

        //Act
        var headers = await _stategy.GetHeadersAsync();

        //Assert
        Assert.NotNull(headers);
        await interceptor1.Received(1).GetHeadersAsync();
        await interceptor2.Received(1).GetHeadersAsync();
        await interceptor3.Received(0).GetHeadersAsync();
        await interceptor1.Received(1).UpdateHeadersAsync(null, headers);
        await interceptor2.Received(0).UpdateHeadersAsync(null, headers);
        await interceptor3.Received(0).UpdateHeadersAsync(null, headers);
        await _authentication.Received(0).AuthenticateAsync();
    }


    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromThirdInterceptor_WithSecondError()
    {
        //Arrange
        var interceptor1 = Substitute.For<IAuthorizationInterceptor>();
        var interceptor2 = Substitute.For<IAuthorizationInterceptor>();
        var interceptor3 = Substitute.For<IAuthorizationInterceptor>();
        interceptor1.GetHeadersAsync().Returns(Task.FromResult<AuthorizationHeaders?>(null));
        interceptor2.GetHeadersAsync().ThrowsAsync(new ArgumentException());
        interceptor3.GetHeadersAsync().Returns(Task.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));
        IAuthorizationInterceptor[] interceptors = [interceptor1, interceptor2, interceptor3];

        _stategy = new AuthorizationInterceptorStrategy(_authentication, _factory, interceptors);

        //Act
        var headers = await _stategy.GetHeadersAsync();

        //Assert
        Assert.NotNull(headers);
        await interceptor1.Received(1).GetHeadersAsync();
        await interceptor2.Received(1).GetHeadersAsync();
        await interceptor3.Received(1).GetHeadersAsync();
        await interceptor1.Received(1).UpdateHeadersAsync(null, headers);
        await interceptor2.Received(1).UpdateHeadersAsync(null, headers);
        await interceptor3.Received(0).UpdateHeadersAsync(null, headers);
        await _authentication.Received(0).AuthenticateAsync();
    }

    [Fact]
    public async Task UpdateHeadersAsync_ShouldNotUpdateInInterceptors()
    {
        //Arrange
        var interceptor1 = Substitute.For<IAuthorizationInterceptor>();
        var interceptor2 = Substitute.For<IAuthorizationInterceptor>();
        var interceptor3 = Substitute.For<IAuthorizationInterceptor>();
        interceptor2.UpdateHeadersAsync(Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>()).ThrowsAsync(new ArgumentException());
        IAuthorizationInterceptor[] interceptors = [interceptor1, interceptor2, interceptor3];
        _stategy = new AuthorizationInterceptorStrategy(_authentication, _factory, interceptors);
        _authentication.UnauthenticateAsync(null).Returns(Task.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));

        //Act
        var act = () => _stategy.UpdateHeadersAsync(null);

        //Assert
        Assert.Null(await Record.ExceptionAsync(act));
        await _authentication.Received(1).UnauthenticateAsync(null);
        await _authentication.Received(0).AuthenticateAsync();
        await interceptor1.Received(1).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
        await interceptor2.Received(1).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
        await interceptor3.Received(1).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
    }



    [Fact]
    public async Task UpdateHeadersAsync_ShouldUpdateInInterceptors()
    {
        //Arrange
        var interceptor1 = Substitute.For<IAuthorizationInterceptor>();
        var interceptor2 = Substitute.For<IAuthorizationInterceptor>();
        var interceptor3 = Substitute.For<IAuthorizationInterceptor>();
        IAuthorizationInterceptor[] interceptors = [interceptor1, interceptor2, interceptor3];
        _stategy = new AuthorizationInterceptorStrategy(_authentication, _factory, interceptors);
        _authentication.UnauthenticateAsync(null).Returns(Task.FromResult<AuthorizationHeaders?>(null));

        //Act
        var act = () => _stategy.UpdateHeadersAsync(null);

        //Assert
        Assert.Null(await Record.ExceptionAsync(act));
        await _authentication.Received(1).UnauthenticateAsync(null);
        await _authentication.Received(0).AuthenticateAsync();
        await interceptor1.Received(0).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
        await interceptor2.Received(0).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
        await interceptor3.Received(0).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
    }
}
