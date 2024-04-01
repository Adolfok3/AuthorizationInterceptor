using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Interceptors;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AuthorizationInterceptor.Tests.Interceptors;

public class AuthorizationInterceptorBaseTests
{
    private readonly IAuthenticationHandler _authentication;
    private readonly ILogger<AuthorizationInterceptorBase> _logger;
    private readonly IAuthorizationInterceptor _next;

    public AuthorizationInterceptorBaseTests()
    {
        _authentication = Substitute.For<IAuthenticationHandler>();
        _logger = Substitute.For<ILogger<AuthorizationInterceptorBase>>();
        _logger.IsEnabled(LogLevel.Debug).Returns(true);
        _next = Substitute.For<IAuthorizationInterceptor>();
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldReturnFrom_Authentication()
    {
        //Arrange
        _authentication.AuthenticateAsync().Returns(new AuthorizationEntry(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        });
        var interceptor = new MockAuthorizationInterceptor(_authentication, _logger, null);

        //Act
        var headers = await interceptor.GetHeadersAsync();

        //Assert
        Assert.NotNull(headers);
        Assert.Contains(headers, a => a.Key == "Authorization");
        Assert.Contains(headers, a => a.Value == "Bearer token");
        Assert.Equal(TimeSpan.FromMinutes(3), headers.ExpiresIn);
        await _authentication.Received(1).AuthenticateAsync();
        await _next.Received(0).GetHeadersAsync();
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldReturnFrom_Next()
    {
        //Arrange
        _next.GetHeadersAsync().Returns(new AuthorizationEntry(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        });
        var interceptor = new MockAuthorizationInterceptor(_authentication, _logger, _next);

        //Act
        var headers = await interceptor.GetHeadersAsync();

        //Assert
        Assert.NotNull(headers);
        Assert.Contains(headers, a => a.Key == "Authorization");
        Assert.Contains(headers, a => a.Value == "Bearer token");
        Assert.Equal(TimeSpan.FromMinutes(3), headers.ExpiresIn);
        await _authentication.Received(0).AuthenticateAsync();
        await _next.Received(1).GetHeadersAsync();
    }

    [Fact]
    public async Task UpdateHeadersAsync_ShouldNotUpdateOnNext()
    {
        //Arrage
        var interceptor = new MockAuthorizationInterceptor(_authentication, _logger, null);
        var entry = MockAuthorizationEntry.CreateEntry();

        //Act
        await interceptor.UpdateHeadersAsync(entry);

        //Assert
        await _next.Received(0).UpdateHeadersAsync(entry);
    }

    [Fact]
    public async Task UpdateHeadersAsync_ShouldUpdateOnNext()
    {
        //Arrage
        var interceptor = new MockAuthorizationInterceptor(_authentication, _logger, _next);
        var entry = MockAuthorizationEntry.CreateEntry();

        //Act
        await interceptor.UpdateHeadersAsync(entry);

        //Assert
        await _next.Received(1).UpdateHeadersAsync(entry);
    }
}
