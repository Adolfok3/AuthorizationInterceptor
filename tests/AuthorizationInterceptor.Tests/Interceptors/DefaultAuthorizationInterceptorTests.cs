using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Interceptors;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AuthorizationInterceptor.Tests.Interceptors;

public class DefaultAuthorizationInterceptorTests
{
    private readonly IAuthenticationHandler _authentication;
    private readonly ILogger<DefaultAuthorizationInterceptor> _logger;
    private DefaultAuthorizationInterceptor _interceptor;

    public DefaultAuthorizationInterceptorTests()
    {
        _authentication = Substitute.For<IAuthenticationHandler>();
        _logger = Substitute.For<ILogger<DefaultAuthorizationInterceptor>>();
        _interceptor = new DefaultAuthorizationInterceptor(_authentication, _logger, null);
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromInner()
    {
        //Arrange
        _authentication.AuthenticateAsync().Returns(new AuthorizationEntry(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        });

        //Act
        var headers = await _interceptor.GetHeadersAsync();

        //
        Assert.NotNull(headers);
        Assert.NotEqual(DateTimeOffset.MinValue, headers.AuthenticatedAt);
        Assert.Equal(TimeSpan.FromMinutes(3), headers.ExpiresIn);
        Assert.Contains(headers, a => a.Key == "Authorization" && a.Value == "Bearer token");
        await _authentication.Received(1).AuthenticateAsync();
    }

    [Fact]
    public async Task UpdateHeadersAsync_ShouldUpdateOnNext()
    {
        //Arrange
        var next = Substitute.For<IAuthorizationInterceptor>();
        _interceptor = new DefaultAuthorizationInterceptor(_authentication, _logger, next);
        var entry = MockAuthorizationEntry.CreateEntry();

        //Act
        await _interceptor.UpdateHeadersAsync(entry);

        //Assert
        await next.Received(1).UpdateHeadersAsync(entry);
    }
}
