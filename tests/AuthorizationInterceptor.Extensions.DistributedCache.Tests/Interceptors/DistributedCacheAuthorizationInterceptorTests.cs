using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Json;
using AuthorizationInterceptor.Extensions.DistributedCache.Interceptors;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace AuthorizationInterceptor.Extensions.DistributedCache.Tests.Interceptors;

public class DistributedCacheAuthorizationInterceptorTests
{
    private const string Key = "authorization_interceptor_distributed_cache_DistributedCacheAuthorizationInterceptor_test";
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheAuthorizationInterceptor _interceptor;

    public DistributedCacheAuthorizationInterceptorTests()
    {
        _cache = Substitute.For<IDistributedCache>();
        _interceptor = new DistributedCacheAuthorizationInterceptor(_cache);
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromCache()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        };
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);
        var bytes = Encoding.UTF8.GetBytes(json);
        _cache.GetAsync(Key).Returns(bytes);

        //Act
        headers = await _interceptor.GetHeadersAsync("test", CancellationToken.None, null);

        //Assert
        headers.Should().NotBeNull();
        headers!.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
        headers.ExpiresIn.Should().Be(TimeSpan.FromMinutes(3));
        headers.Should().Contain(a => a.Key == "Authorization" && a.Value == "Bearer token");
        await _cache.Received(1).GetAsync(Key);
        await _cache.Received(0).SetAsync(Key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task GetHeadersAsync_WithOAuth_ShouldGetFromCache()
    {
        //Arrange
        AuthorizationHeaders? headers = new OAuthHeaders("token", "type", 123, "refresh", 12345);
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);
        var bytes = Encoding.UTF8.GetBytes(json);
        _cache.GetAsync(Key).Returns(bytes);

        //Act
        headers = await _interceptor.GetHeadersAsync("test", CancellationToken.None, null);

        //Assert
        headers.Should().NotBeNull();
        headers.Should().NotBeEmpty();
        headers!.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
        headers.ExpiresIn.Should().Be(TimeSpan.FromSeconds(12345));
        headers.Should().Contain(a => a.Key == "Authorization" && a.Value == "type token");
        headers.OAuthHeaders.Should().NotBeNull();
        await _cache.Received(1).GetAsync(Key);
        await _cache.Received(0).SetAsync(Key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromCache_AndReturnNull()
    {
        //Arrange
        _cache.GetAsync(Key).Returns(Task.FromResult<byte[]?>(null));

        //Act
        var headers = await _interceptor.GetHeadersAsync("test", CancellationToken.None, null);

        //Assert
        headers.Should().BeNull();
        await _cache.Received(1).GetAsync(Key);
        await _cache.Received(0).SetAsync(Key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
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
        var act = async () => await _interceptor.UpdateHeadersAsync("test", null, headers, CancellationToken.None, null);

        //Assert
        await act.Should().NotThrowAsync();
        await _cache.Received(1).SetAsync(Key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task UpdateHeadersAsync_WithNullHeaders_ShouldNotUpdate()
    {
        //Act
        var act = async () => await _interceptor.UpdateHeadersAsync("test", null, null, CancellationToken.None, null);

        //Assert
        await act.Should().NotThrowAsync();
        await _cache.Received(0).SetAsync(Key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task GetHeadersAsync_WithCacheKeySuffix_ShouldGetFromCache()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        };
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);
        var bytes = Encoding.UTF8.GetBytes(json);
        _cache.GetAsync($"{Key}_some-suffix").Returns(bytes);

        //Act
        headers = await _interceptor.GetHeadersAsync("test", CancellationToken.None, "some-suffix");

        //Assert
        headers.Should().NotBeNull();
        headers!.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
        headers.ExpiresIn.Should().Be(TimeSpan.FromMinutes(3));
        headers.Should().Contain(a => a.Key == "Authorization" && a.Value == "Bearer token");
        await _cache.Received(1).GetAsync($"{Key}_some-suffix");
        await _cache.Received(0).SetAsync($"{Key}_some-suffix", Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task GetHeadersAsync_WithOAuth_WithCacheKeySuffix_ShouldGetFromCache()
    {
        //Arrange
        AuthorizationHeaders? headers = new OAuthHeaders("token", "type", 123, "refresh", 12345);
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);
        var bytes = Encoding.UTF8.GetBytes(json);
        _cache.GetAsync($"{Key}_some-suffix").Returns(bytes);

        //Act
        headers = await _interceptor.GetHeadersAsync("test", CancellationToken.None, "some-suffix");

        //Assert
        headers.Should().NotBeNull();
        headers.Should().NotBeEmpty();
        headers!.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
        headers.ExpiresIn.Should().Be(TimeSpan.FromSeconds(12345));
        headers.Should().Contain(a => a.Key == "Authorization" && a.Value == "type token");
        headers.OAuthHeaders.Should().NotBeNull();
        await _cache.Received(1).GetAsync($"{Key}_some-suffix");
        await _cache.Received(0).SetAsync($"{Key}_some-suffix", Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task GetHeadersAsync_ShouldGetFromCache_WithCacheKeySuffix_AndReturnNull()
    {
        //Arrange
        _cache.GetAsync(Key).Returns(Task.FromResult<byte[]?>(null));

        //Act
        var headers = await _interceptor.GetHeadersAsync("test", CancellationToken.None, "some-suffix");

        //Assert
        headers.Should().BeNull();
        await _cache.Received(1).GetAsync($"{Key}_some-suffix");
        await _cache.Received(0).SetAsync($"{Key}_some-suffix", Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task UpdateHeadersAsync_WithCacheKeySuffix_ShouldUpdateSuccessfully()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Authorization", "Bearer token" }
        };

        //Act
        var act = async () => await _interceptor.UpdateHeadersAsync("test", null, headers, CancellationToken.None, "some-suffix");

        //Assert
        await act.Should().NotThrowAsync();
        await _cache.Received(1).SetAsync($"{Key}_some-suffix", Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }

    [Fact]
    public async Task UpdateHeadersAsync_WithNullHeaders_WithCacheKeySuffix_ShouldNotUpdate()
    {
        //Act
        var act = async () => await _interceptor.UpdateHeadersAsync("test", null, null, CancellationToken.None, "some-suffix");

        //Assert
        await act.Should().NotThrowAsync();
        await _cache.Received(0).SetAsync(Key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>());
    }
}
