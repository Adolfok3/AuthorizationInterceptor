using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Log;
using AuthorizationInterceptor.Strategies;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Tests.Handlers;

public class AuthorizationInterceptorHandlerTests
{
    private readonly ILogger _logger;
    private readonly IAuthorizationInterceptorStrategy _strategy;
    private readonly IAuthenticationHandler _authenticationHandler;
    private readonly HttpClient _client;

    public AuthorizationInterceptorHandlerTests()
    {
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(LogLevel.Debug).Returns(true);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger("AuthorizationInterceptorHandler").Returns(_logger);


        Func<HttpResponseMessage, bool> func = f => f.StatusCode == System.Net.HttpStatusCode.Unauthorized;
        _strategy = Substitute.For<IAuthorizationInterceptorStrategy>();
        _authenticationHandler = Substitute.For<IAuthenticationHandler>();

        var handler = new AuthorizationInterceptorHandler("test", func, _authenticationHandler, _strategy, loggerFactory);
        handler.InnerHandler = new MockAuthorizationInterceptorHandler();
        _client = new HttpClient(handler);
    }

    [Fact]
    public void SendSync_ShouldLogWarning()
    {
        //Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://somesite.com");

        //Act
        _client.Send(request);

        //Assert
        _logger.Received(1).LogUnavailableForSyncRequests();
    }

    [Fact]
    public async Task SendAsync_WithoutHeaders_ShouldSendRequestCorrectly()
    {
        //Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://somesite.com");

        //Act
        var response = await _client.SendAsync(request, CancellationToken.None);

        //Assert
        Assert.True(response.IsSuccessStatusCode);
        await _strategy.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders>(), _authenticationHandler, Arg.Any<CancellationToken>());
        await _strategy.Received(1).GetHeadersAsync("test", _authenticationHandler, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithHeaders_ShouldSendRequestCorrectly()
    {
        //Arrange
        _strategy.GetHeadersAsync("test", _authenticationHandler, Arg.Any<CancellationToken>()).Returns(new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        });
        var request = new HttpRequestMessage(HttpMethod.Get, "http://somesite.com");

        //Act
        var response = await _client.SendAsync(request, CancellationToken.None);

        //Assert
        Assert.True(response.IsSuccessStatusCode);
        await _strategy.Received(0).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders>(), _authenticationHandler, Arg.Any<CancellationToken>());
        await _strategy.Received(1).GetHeadersAsync("test", _authenticationHandler, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithHeaders_ShouldReturnsAnauthorized()
    {
        //Arrange
        _strategy.GetHeadersAsync("test", _authenticationHandler, Arg.Any<CancellationToken>()).Returns(new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "ShouldReturnUnauthorized", "ShouldReturnUnauthorized" }
        });
        var request = new HttpRequestMessage(HttpMethod.Get, "http://somesite.com");

        //Act
        var response = await _client.SendAsync(request, CancellationToken.None);

        //Assert
        Assert.False(response.IsSuccessStatusCode);
        await _strategy.Received(1).UpdateHeadersAsync("test", Arg.Any<AuthorizationHeaders>(), _authenticationHandler, Arg.Any<CancellationToken>());
        await _strategy.Received(1).GetHeadersAsync("test", _authenticationHandler, Arg.Any<CancellationToken>());
    }
}
