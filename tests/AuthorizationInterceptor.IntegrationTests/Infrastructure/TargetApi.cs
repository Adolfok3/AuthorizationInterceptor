using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.IntegrationTests.Infrastructure;

/// <summary>
/// In-memory target API (hosted via TestServer) that models the API being authenticated against.
/// <c>POST /auth</c> increments the <see cref="LoginCounter"/> and issues a token; <c>GET /data</c> is a
/// protected resource that returns 401 without a known bearer token. An artificial delay on <c>/auth</c>
/// widens the concurrency window so the "single login" guarantee is actually exercised.
/// </summary>
public sealed class TargetApi : IAsyncDisposable
{
    private readonly WebApplication _app;

    public LoginCounter Counter { get; }

    private TargetApi(WebApplication app, LoginCounter counter)
    {
        _app = app;
        Counter = counter;
    }

    public static async Task<TargetApi> StartAsync(TimeSpan authDelay)
    {
        var counter = new LoginCounter();
        var issuedTokens = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(counter);

        var app = builder.Build();

        app.MapPost("/auth", async () =>
        {
            counter.Increment();

            if (authDelay > TimeSpan.Zero)
                await Task.Delay(authDelay);

            var accessToken = Guid.NewGuid().ToString("N");
            issuedTokens[accessToken] = 1;

            return Results.Json(new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 60,
                refresh_token = Guid.NewGuid().ToString("N"),
                refresh_token_expires_in = 120
            });
        });

        app.MapGet("/data", (HttpContext context) =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization))
                return Results.Unauthorized();

            var token = authorization.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return issuedTokens.ContainsKey(token) ? Results.Ok("ok") : Results.Unauthorized();
        });

        await app.StartAsync();

        return new TargetApi(app, counter);
    }

    public HttpMessageHandler CreateHandler() => _app.GetTestServer().CreateHandler();

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
