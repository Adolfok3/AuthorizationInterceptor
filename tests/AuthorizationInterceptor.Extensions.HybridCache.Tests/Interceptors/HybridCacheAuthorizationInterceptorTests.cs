using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Json;
using AuthorizationInterceptor.Extensions.HybridCache.Interceptors;
using Microsoft.Extensions.Caching.Hybrid;

namespace AuthorizationInterceptor.Extensions.HybridCache.Tests.Interceptors;

public class HybridCacheAuthorizationInterceptorTests
{
    private const string Key = "authorization_interceptor_hybrid_cache_HybridCacheAuthorizationInterceptor_test";
    private readonly Microsoft.Extensions.Caching.Hybrid.HybridCache _hybridCache;
    private readonly HybridCacheAuthorizationInterceptor _sut;

    public HybridCacheAuthorizationInterceptorTests()
    {
        _hybridCache = Substitute.For<Microsoft.Extensions.Caching.Hybrid.HybridCache>();
        _sut = new HybridCacheAuthorizationInterceptor(_hybridCache);
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromCache()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        };
        var cache = AuthorizationHeadersJsonSerializer.Serialize(headers);
        _hybridCache.GetOrCreateAsync(Key, Arg.Any<Func<CancellationToken, ValueTask<string?>>>(), Arg.Any<HybridCacheEntryOptions>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(cache));

        //Act
        var result = await _sut.GetHeadersAsync("test", CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
        result!.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
        result.ExpiresIn.Should().Be(TimeSpan.FromMinutes(3));
        result.Should().Contain(a => a.Key == "Authorization" && a.Value == "Bearer token");
        await _hybridCache.Received(1).GetOrCreateAsync(Key, Arg.Any<Func<CancellationToken, ValueTask<string?>>>(), Arg.Any<HybridCacheEntryOptions>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsync_WithOAuth_ShouldGetFromCache()
    {
        //Arrange
        AuthorizationHeaders? headers = new OAuthHeaders("token", "type", 123, "refresh", 12345);
        var cache = AuthorizationHeadersJsonSerializer.Serialize(headers);
        _hybridCache.GetOrCreateAsync(Key, Arg.Any<Func<CancellationToken, ValueTask<string?>>>(), Arg.Any<HybridCacheEntryOptions>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(cache));

        //Act
        headers = await _sut.GetHeadersAsync("test", CancellationToken.None);

        //Assert
        headers.Should().NotBeNull();
        headers.Should().NotBeEmpty();
        headers!.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
        headers.ExpiresIn.Should().Be(TimeSpan.FromSeconds(12345));
        headers.Should().Contain(a => a.Key == "Authorization" && a.Value == "type token");
        headers.OAuthHeaders.Should().NotBeNull();
        await _hybridCache.Received(1).GetOrCreateAsync(Key, Arg.Any<Func<CancellationToken, ValueTask<string?>>>(), Arg.Any<HybridCacheEntryOptions>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromCache_AndReturnNull()
    {
        //Act
        var headers = await _sut.GetHeadersAsync("test", CancellationToken.None);

        //Assert
        headers.Should().BeNull();
        await _hybridCache.Received(1).GetOrCreateAsync(Key, Arg.Any<Func<CancellationToken, ValueTask<string?>>>(), Arg.Any<HybridCacheEntryOptions>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateHeadersAsync_ShouldUpdateSuccessfully()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        };

        //Act
        var act = async () => await _sut.UpdateHeadersAsync("test", null, headers, CancellationToken.None);

        //Assert
        await act.Should().NotThrowAsync();
        await _hybridCache.Received(1).SetAsync(Key, Arg.Any<string>(), Arg.Any<HybridCacheEntryOptions>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateHeadersAsync_WithNullHeaders_ShouldNotUpdate()
    {
        //Act
        var act = async () => await _sut.UpdateHeadersAsync("test", null, null, CancellationToken.None);

        //Assert
        await act.Should().NotThrowAsync();
        await _hybridCache.Received(0).SetAsync(Key, Arg.Any<string>(), Arg.Any<HybridCacheEntryOptions>(), null, Arg.Any<CancellationToken>());
    }
}
