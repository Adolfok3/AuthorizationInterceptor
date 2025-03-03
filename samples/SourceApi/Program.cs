using AuthorizationInterceptor.Extensions;
using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("TargetApiAuth")
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuthClass>(opt =>
    {
        opt.UseCustomInterceptor<CustomInterceptor1>();
        opt.UseCustomInterceptor<CustomInterceptor2>();
        opt.UseCustomInterceptor<CustomInterceptor3>();
    })
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5121"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/data", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("TargetApi");
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
