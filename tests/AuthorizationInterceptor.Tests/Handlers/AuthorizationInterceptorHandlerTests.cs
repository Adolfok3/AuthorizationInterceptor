using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Interceptors;
using AuthorizationInterceptor.Options;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AuthorizationInterceptor.Tests.Handlers;

public class AuthorizationInterceptorHandlerTests
{
    private readonly IAuthorizationInterceptor interceptor;
    private readonly HttpClient _client;

    public AuthorizationInterceptorHandlerTests()
    {
        var logger = Substitute.For<ILogger<AuthorizationInterceptorHandler>>();
        logger.IsEnabled(LogLevel.Debug).Returns(true);
        interceptor = Substitute.For<IAuthorizationInterceptor>();
        var handler = new AuthorizationInterceptorHandler(interceptor, new AuthorizationInterceptorOptions(), logger);
        handler.InnerHandler = new MockAuthorizationInterceptorHandler(interceptor, new AuthorizationInterceptorOptions(), logger);
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
        await interceptor.Received(0).UpdateHeadersAsync(Arg.Any<AuthorizationEntry>());
        await interceptor.Received(1).GetHeadersAsync();
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
        await interceptor.Received(0).UpdateHeadersAsync(Arg.Any<AuthorizationEntry>());
        await interceptor.Received(1).GetHeadersAsync();
    }

    [Fact]
    public async Task SendAsync_WithHeaders_ShouldSendRequestCorrectly()
    {
        //Arrange
        interceptor.GetHeadersAsync().Returns(new AuthorizationEntry(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        });
        var request = new HttpRequestMessage(HttpMethod.Get, "http://somesite.com");

        //Act
        var response = await _client.SendAsync(request, CancellationToken.None);

        //Assert
        Assert.True(response.IsSuccessStatusCode);
        await interceptor.Received(0).UpdateHeadersAsync(Arg.Any<AuthorizationEntry>());
        await interceptor.Received(1).GetHeadersAsync();
    }

    [Fact]
    public async Task SendAsync_WithHeaders_ShouldReturnsAnauthorized()
    {
        //Arrange
        interceptor.GetHeadersAsync().Returns(new AuthorizationEntry(TimeSpan.FromMinutes(3))
        {
            { "ShouldReturnUnauthorized", "ShouldReturnUnauthorized" }
        });
        var request = new HttpRequestMessage(HttpMethod.Get, "http://somesite.com");

        //Act
        var response = await _client.SendAsync(request, CancellationToken.None);

        //Assert
        Assert.False(response.IsSuccessStatusCode);
        await interceptor.Received(1).UpdateHeadersAsync(Arg.Any<AuthorizationEntry>());
        await interceptor.Received(1).GetHeadersAsync();
    }
}
