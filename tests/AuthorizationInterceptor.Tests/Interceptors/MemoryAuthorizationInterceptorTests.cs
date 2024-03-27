using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Interceptors;
using AuthorizationInterceptor.Tests.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AuthorizationInterceptor.Tests.Interceptors;

public class MemoryAuthorizationInterceptorTests
{
    private const string KEY = "authorization_interceptor_memory_cache_ObjectProxy";
    private readonly IAuthenticationHandler _authentication;
    private readonly IMemoryCache _memory;
    private readonly ILogger<MemoryAuthorizationInterceptor> _logger;
    private MemoryAuthorizationInterceptor _interceptor;

    public MemoryAuthorizationInterceptorTests()
    {
        _authentication = Substitute.For<IAuthenticationHandler>();
        _memory = Substitute.For<IMemoryCache>();
        _logger = Substitute.For<ILogger<MemoryAuthorizationInterceptor>>();
        _interceptor = new MemoryAuthorizationInterceptor(_authentication, _memory, _logger, null);
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
        _memory.Received(1).TryGetValue(Arg.Is<string>(i => i.StartsWith(KEY)), out var _);
    }

    [Fact]
    public async Task UpdateHeadersAsync_ShouldUpdateOnMemoryAndNext()
    {
        //Arrange
        var next = Substitute.For<IAuthorizationInterceptor>();
        _interceptor = new MemoryAuthorizationInterceptor(_authentication, _memory, _logger, next);
        var entry = MockAuthorizationEntry.CreateEntry();
        next.UpdateHeadersAsync(entry).Returns(entry);

        //Act
        await _interceptor.UpdateHeadersAsync(entry);

        //Assert
        _memory.Received(1).Remove(Arg.Is<string>(i => i.StartsWith(KEY)));
        _memory.Received(1).TryGetValue(Arg.Is<string>(i => i.StartsWith(KEY)), out var _);
        await next.Received(1).UpdateHeadersAsync(entry);
    }
}
