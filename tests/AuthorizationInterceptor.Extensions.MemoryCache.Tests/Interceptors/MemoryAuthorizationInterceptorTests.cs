using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.MemoryCache.Interceptors;
using Microsoft.Extensions.Caching.Memory;

namespace AuthorizationInterceptor.Extensions.MemoryCache.Tests.Interceptors;

public class MemoryAuthorizationInterceptorTests
{
    private const string CacheKey = "authorization_interceptor_memory_cache_MemoryCacheInterceptor_test";

    private readonly IMemoryCache _memory;
    private readonly MemoryCacheInterceptor _interceptor;

    public MemoryAuthorizationInterceptorTests()
    {
        _memory = Substitute.For<IMemoryCache>();
        _interceptor = new MemoryCacheInterceptor(_memory);
    }

    [Fact]
    public async Task GetHeadersAsyncShouldReturnNull()
    {
        //Act
        var headers = await _interceptor.GetHeadersAsync("test", CancellationToken.None);

        //Assert
        headers.Should().BeNull();
        _memory.Received(1).Get<AuthorizationHeaders?>(CacheKey);
    }

    [Fact]
    public async Task GetHeadersAsyncShouldReturnHeaders()
    {
        //Arrange
        AuthorizationHeaders obj = new OAuthHeaders("accesstoken", "tokentype");
        _memory.TryGetValue(CacheKey, out Arg.Any<object?>()).Returns(x =>
        {
            x[1] = obj;
            return true;
        });

        //Act
        var headers = await _interceptor.GetHeadersAsync("test", CancellationToken.None);

        //Assert
        headers.Should().NotBeNull();
        headers!.OAuthHeaders.Should().NotBeNull();
        headers.OAuthHeaders!.AccessToken.Should().Be("accesstoken");
        headers.OAuthHeaders.TokenType.Should().Be("tokentype");
        _memory.Received(1).Get<AuthorizationHeaders?>(CacheKey);
    }

    [Fact]
    public void UpdateHeadersAsyncWithNullHeadersShouldNotUpdateInMemoryCache()
    {
        //Act
        var act = () => _interceptor.UpdateHeadersAsync("test", null, null, CancellationToken.None);

        //Assert
        act.Should().NotThrowAsync();
        _memory.Received(0).CreateEntry(Arg.Any<object>());
    }

    [Fact]
    public void UpdateHeadersAsyncWithHeadersShouldUpdateInMemoryCache()
    {
        //Act
        var act = () => _interceptor.UpdateHeadersAsync("test", null, new OAuthHeaders("accesstoken", "tokentype"), CancellationToken.None);

        //Assert
        act.Should().NotThrowAsync();
        _memory.Received(1).CreateEntry(Arg.Any<object>());
    }

    [Fact]
    public async Task GetHeadersAsyncWithCacheKeySuffixShouldReturnNull()
    {
        //Act
        var headers = await _interceptor.GetHeadersAsync("test_some-suffix", CancellationToken.None);

        //Assert
        headers.Should().BeNull();
        _memory.Received(1).Get<AuthorizationHeaders?>($"{CacheKey}_some-suffix");
    }

    [Fact]
    public async Task GetHeadersAsyncWithCacheKeySuffixShouldReturnHeaders()
    {
        //Arrange
        AuthorizationHeaders obj = new OAuthHeaders("accesstoken", "tokentype");
        _memory.TryGetValue($"{CacheKey}_some-suffix", out Arg.Any<object?>()).Returns(x =>
        {
            x[1] = obj;
            return true;
        });

        //Act
        var headers = await _interceptor.GetHeadersAsync("test_some-suffix", CancellationToken.None);

        //Assert
        headers.Should().NotBeNull();
        headers!.OAuthHeaders.Should().NotBeNull();
        headers.OAuthHeaders!.AccessToken.Should().Be("accesstoken");
        headers.OAuthHeaders.TokenType.Should().Be("tokentype");
        _memory.Received(1).Get<AuthorizationHeaders?>($"{CacheKey}_some-suffix");
    }

    [Fact]
    public void UpdateHeadersAsyncWithNullHeadersWithCacheKeySuffixShouldNotUpdateInMemoryCache()
    {
        //Act
        var act = () => _interceptor.UpdateHeadersAsync("test_some-suffix", null, null, CancellationToken.None);

        //Assert
        act.Should().NotThrowAsync();
        _memory.Received(0).CreateEntry(Arg.Any<object>());
    }

    [Fact]
    public void UpdateHeadersAsyncWithHeadersWithCacheKeySuffixShouldUpdateInMemoryCache()
    {
        //Act
        var act = () => _interceptor.UpdateHeadersAsync("test_some-suffix", null, new OAuthHeaders("accesstoken", "tokentype"), CancellationToken.None);

        //Assert
        act.Should().NotThrowAsync();
        _memory.Received(1).CreateEntry(Arg.Any<object>());
    }
}
