using AuthorizationInterceptor.Extensions;
using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.DistributedCache.Extensions;
using AuthorizationInterceptor.Extensions.HybridCache.Extensions;
using AuthorizationInterceptor.Extensions.MemoryCache.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();

// Add the cache options
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.InstanceName = "SourceApi";
    opt.Configuration = "localhost:6379";
});
builder.Services.AddHybridCache();

builder.Services.AddHttpClient("TargetApiAuth")
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

builder.Services.AddHttpClient("TargetApiWithNoInterceptor")
    .AddAuthorizationInterceptorHandler<TargetApiAuthClass>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

builder.Services.AddHttpClient("TargetApiWithMemoryCache")
    .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(opt =>
    {
        opt.UseMemoryCacheInterceptor();
    })
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

builder.Services.AddHttpClient("TargetApiWithDistributedCache")
    .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(opt =>
    {
        opt.UseDistributedCacheInterceptor();
    })
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

builder.Services.AddHttpClient("TargetApiWithHybridCache")
    .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(opt =>
    {
        opt.UseHybridCacheInterceptor();
    })
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

builder.Services.AddHttpClient("TargetApiWithCustomInterceptors")
    .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(opt =>
    {
        opt.UseMemoryCacheInterceptor();
        opt.UseDistributedCacheInterceptor();

        opt.UseCustomInterceptor<CustomInterceptor1>();
        opt.UseCustomInterceptor<CustomInterceptor2>();
        opt.UseCustomInterceptor<CustomInterceptor3>();
    })
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

builder.Services.AddHttpClient("TargetApiWithData1")
    .AddAuthorizationInterceptorHandler(provider => ActivatorUtilities.CreateInstance<TargetApiWithDataAuthClass>(provider, new SomeData("data1")))
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

builder.Services.AddHttpClient("TargetApiWithData2")
    .AddAuthorizationInterceptorHandler(provider => ActivatorUtilities.CreateInstance<TargetApiWithDataAuthClass>(provider, new SomeData("data2")))
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

var app = builder.Build();
app.UseOpenApi();
app.UseSwaggerUi();

app.MapGet("/test/TargetApiWithNoInterceptor", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("TargetApiWithNoInterceptor");
    return await client.GetAsync("/data");
});

app.MapGet("/test/TargetApiWithMemoryCache", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("TargetApiWithMemoryCache");
    return await client.GetAsync("/data");
});

app.MapGet("/test/TargetApiWithDistributedCache", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("TargetApiWithDistributedCache");
    return await client.GetAsync("/data");
});

app.MapGet("/test/TargetApiWithHybridCache", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("TargetApiWithHybridCache");
    return await client.GetAsync("/data");
});

app.MapGet("/test/TargetApiWithCustomInterceptors", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("TargetApiWithCustomInterceptors");
    return await client.GetAsync("/data");
});

app.MapGet("/test/TargetApiWithData1", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("TargetApiWithData1");
    return await client.GetAsync("/data");
});

app.MapGet("/test/TargetApiWithData2", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("TargetApiWithData2");
    return await client.GetAsync("/data");
});

app.Run();

public class User
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token_expires_in")]
    public int RefreshTokenExpiresIn { get; set; }
}

public class TargetApiAuthClass : IAuthenticationHandler
{
    private readonly HttpClient _client;

    public TargetApiAuthClass(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("TargetApiAuth");
    }

    public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(AuthorizationHeaders? expiredHeaders, CancellationToken cancellationToken)
    {
        var response = expiredHeaders != null && expiredHeaders.OAuthHeaders != null
            ? await _client.PostAsync($"refresh?refresh={expiredHeaders.OAuthHeaders?.RefreshToken}", null)
            : await _client.PostAsync("auth", null);

        var content = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<User>(content);
        return new OAuthHeaders(user.AccessToken, user.TokenType, user.ExpiresIn, user.RefreshToken, user.RefreshTokenExpiresIn);
    }
}

public class TargetApiWithDataAuthClass : IAuthenticationHandler
{
    private readonly SomeData _data;
    private readonly HttpClient _client;

    public TargetApiWithDataAuthClass(SomeData data, IHttpClientFactory httpClientFactory)
    {
        _data = data;
        _client = httpClientFactory.CreateClient("TargetApiAuth");
    }

    public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(AuthorizationHeaders? expiredHeaders, CancellationToken cancellationToken)
    {
        var response = expiredHeaders != null && expiredHeaders.OAuthHeaders != null
            ? await _client.PostAsync($"refresh?refresh={expiredHeaders.OAuthHeaders?.RefreshToken}&data={_data.Data}", null)
            : await _client.PostAsync($"auth?data={_data.Data}", null);

        var content = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<User>(content);
        return new OAuthHeaders(user.AccessToken, user.TokenType, user.ExpiresIn, user.RefreshToken, user.RefreshTokenExpiresIn);
    }
}

public class CustomInterceptor1 : IAuthorizationInterceptor
{
    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<AuthorizationHeaders?>(null);
    }

    public ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

public class CustomInterceptor2 : IAuthorizationInterceptor
{
    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<AuthorizationHeaders?>(null);
    }

    public ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

public class CustomInterceptor3 : IAuthorizationInterceptor
{
    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<AuthorizationHeaders?>(null);
    }

    public ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

public record SomeData(string Data);
