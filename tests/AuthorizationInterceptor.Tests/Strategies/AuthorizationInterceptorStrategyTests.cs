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
    private readonly IAuthorizationInterceptor _interceptor1;
    private readonly IAuthorizationInterceptor _interceptor2;
    private readonly IAuthorizationInterceptor _interceptor3;
    private IAuthorizationInterceptorStrategy _stategy;

    public AuthorizationInterceptorStrategyTests()
    {
        _interceptor1 = Substitute.For<IAuthorizationInterceptor>();
        _interceptor2 = Substitute.For<IAuthorizationInterceptor>();
        _interceptor3 = Substitute.For<IAuthorizationInterceptor>();
        var logger = Substitute.For<ILogger<AuthorizationInterceptorStrategy>>();
        logger.IsEnabled(LogLevel.Debug).Returns(true);
        _stategy = new AuthorizationInterceptorStrategy(logger, [_interceptor1, _interceptor2, _interceptor3]);
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetOnlyInAuthenticationHandler()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        authentication.AuthenticateAsync().Returns(Task.FromResult<AuthorizationHeaders?>(null));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication);

        //Assert
        Assert.Null(headers);
        await authentication.Received(1).AuthenticateAsync();
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromSecondInterceptor()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor1.GetHeadersAsync("test").Returns(Task.FromResult<AuthorizationHeaders?>(null));
        _interceptor2.GetHeadersAsync("test").Returns(Task.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));
        _interceptor3.GetHeadersAsync("test").Returns(Task.FromResult<AuthorizationHeaders?>(null));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication);

        //Assert
        Assert.NotNull(headers);
        await _interceptor1.Received(1).GetHeadersAsync("test");
        await _interceptor2.Received(1).GetHeadersAsync("test");
        await _interceptor3.Received(0).GetHeadersAsync("test");
        await _interceptor1.Received(1).UpdateHeadersAsync("test", null, headers);
        await _interceptor2.Received(0).UpdateHeadersAsync("test", null, headers);
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers);
        await authentication.Received(0).AuthenticateAsync();
    }


    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromThirdInterceptor_WithSecondError()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor1.GetHeadersAsync("test").Returns(Task.FromResult<AuthorizationHeaders?>(null));
        _interceptor2.GetHeadersAsync("test").ThrowsAsync(new ArgumentException());
        _interceptor3.GetHeadersAsync("test").Returns(Task.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));

        //Act
        var headers = await _stategy.GetHeadersAsync("test", authentication);

        //Assert
        Assert.NotNull(headers);
        await _interceptor1.Received(1).GetHeadersAsync("test");
        await _interceptor2.Received(1).GetHeadersAsync("test");
        await _interceptor3.Received(1).GetHeadersAsync("test");
        await _interceptor1.Received(1).UpdateHeadersAsync("test", null, headers);
        await _interceptor2.Received(1).UpdateHeadersAsync("test", null, headers);
        await _interceptor3.Received(0).UpdateHeadersAsync("test", null, headers);
        await authentication.Received(0).AuthenticateAsync();
    }

    [Fact]
    public async Task UpdateHeadersAsync_ShouldNotUpdateInSecondInterceptors()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        _interceptor2.UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>()).ThrowsAsync(new ArgumentException());
        authentication.UnauthenticateAsync(null).Returns(Task.FromResult<AuthorizationHeaders?>(MockAuthorizationHeaders.CreateHeaders()));

        //Act
        var act = () => _stategy.UpdateHeadersAsync("test", null, authentication);

        //Assert
        Assert.Null(await Record.ExceptionAsync(act));
        await authentication.Received(1).UnauthenticateAsync(null);
        await authentication.Received(0).AuthenticateAsync();
        await _interceptor1.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
        await _interceptor2.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
        await _interceptor3.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
    }



    [Fact]
    public async Task UpdateHeadersAsync_ShouldUpdateInInterceptors()
    {
        //Arrange
        var authentication = Substitute.For<IAuthenticationHandler>();
        authentication.UnauthenticateAsync(null).Returns(Task.FromResult<AuthorizationHeaders?>(null));

        //Act
        var act = () => _stategy.UpdateHeadersAsync("test", null, authentication);

        //Assert
        Assert.Null(await Record.ExceptionAsync(act));
        await authentication.Received(1).UnauthenticateAsync(null);
        await authentication.Received(0).AuthenticateAsync();
        await _interceptor1.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
        await _interceptor2.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
        await _interceptor3.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders?>(), Arg.Any<AuthorizationHeaders?>());
    }
}
