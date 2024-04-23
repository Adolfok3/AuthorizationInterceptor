using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Options;
using AuthorizationInterceptor.Strategies;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AuthorizationInterceptor.Tests.Handlers;

public class AuthorizationInterceptorHandlerTests
{
    private readonly IAuthorizationInterceptorStrategy _strategy;
    private readonly HttpClient _client;

    public AuthorizationInterceptorHandlerTests()
    {
        var factory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Debug).Returns(true);
        factory.CreateLogger("AuthorizationInterceptorHandler").Returns(logger);

        _strategy = Substitute.For<IAuthorizationInterceptorStrategy>();

        var handler = new AuthorizationInterceptorHandler(new AuthorizationInterceptorOptions(), _strategy, factory);
        handler.InnerHandler = new MockAuthorizationInterceptorHandler();
        _client = new HttpClient(handler);
    }

    [Fact]
    public async Task Send_WithoutHeaders_ShouldSendRequestCorrectly()
    {
        //Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://somesite.com");

        //Act
        var response = _client.Send(request, CancellationToken.None);

        //Assert
        Assert.True(response.IsSuccessStatusCode);
        await _strategy.Received(0).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders>());
        await _strategy.Received(1).GetHeadersAsync();
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
        await _strategy.Received(0).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders>());
        await _strategy.Received(1).GetHeadersAsync();
    }

    [Fact]
    public async Task SendAsync_WithHeaders_ShouldSendRequestCorrectly()
    {
        //Arrange
        _strategy.GetHeadersAsync().Returns(new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        });
        var request = new HttpRequestMessage(HttpMethod.Get, "http://somesite.com");

        //Act
        var response = await _client.SendAsync(request, CancellationToken.None);

        //Assert
        Assert.True(response.IsSuccessStatusCode);
        await _strategy.Received(0).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders>());
        await _strategy.Received(1).GetHeadersAsync();
    }

    [Fact]
    public async Task SendAsync_WithHeaders_ShouldReturnsAnauthorized()
    {
        //Arrange
        _strategy.GetHeadersAsync().Returns(new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "ShouldReturnUnauthorized", "ShouldReturnUnauthorized" }
        });
        var request = new HttpRequestMessage(HttpMethod.Get, "http://somesite.com");

        //Act
        var response = await _client.SendAsync(request, CancellationToken.None);

        //Assert
        Assert.False(response.IsSuccessStatusCode);
        await _strategy.Received(1).UpdateHeadersAsync(Arg.Any<AuthorizationHeaders>());
        await _strategy.Received(1).GetHeadersAsync();
    }
}
