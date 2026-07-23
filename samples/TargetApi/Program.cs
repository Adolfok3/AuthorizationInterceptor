using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddSingleton(new UserContainer());

var app = builder.Build();
app.UseOpenApi();
app.UseSwaggerUi();

app.MapPost("/auth", ([FromHeader(Name = "x-mycustom-header")]string? myCustomHeader, UserContainer users, ILoggerFactory loggerFactory, HttpContext httpContext) =>
{
    var test = httpContext.Request.Headers;
    var logger = loggerFactory.CreateLogger("TargetApi");
    logger.LogDebug("Received request on /auth endpoint");
    var user = new User
    {
        Name = myCustomHeader ?? "Unknown User",
        AccessToken = Guid.NewGuid().ToString(),
        RefreshToken = Guid.NewGuid().ToString(),
        TokenType = "Bearer",
        ExpiresIn = 15,
        RefreshTokenExpiresIn = 30,
        ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15),
    };

    users.Users.Add(user);
    return user;
})
.WithName("auth");

app.MapPost("/refresh", ([FromHeader(Name = "x-mycustom-header")]string? myCustomHeader, UserContainer users, [FromQuery] string refresh, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("TargetApi");
    logger.LogDebug("Received request on /refresh endpoint");

    if (string.IsNullOrEmpty(refresh))
        return Results.Unauthorized();

    var user = users.Users.FirstOrDefault(f => f.RefreshToken == refresh);
    if (user == null)
        return Results.Unauthorized();

    user = new User
    {
        Name = myCustomHeader ?? "Unknown User",
        AccessToken = Guid.NewGuid().ToString(),
        RefreshToken = Guid.NewGuid().ToString(),
        TokenType = "Bearer",
        ExpiresIn = 15,
        RefreshTokenExpiresIn = 30,
        ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15)
    };

    users.Users.Add(user);
    return Results.Ok(user);
})
.WithName("refresh");

app.MapGet("/data", (HttpRequest request, UserContainer users, ILoggerFactory loggerFactory, [FromHeader(Name = "Authorization")] string? token = null) =>
{
    var logger = loggerFactory.CreateLogger("TargetApi");
    logger.LogDebug("Received request on /data endpoint");

    if (string.IsNullOrWhiteSpace(token))
        return Results.Unauthorized();

    token = token.Replace("Bearer ", string.Empty);
    var user = users.Users.FirstOrDefault(a => a.AccessToken == token && DateTimeOffset.UtcNow < a.ExpiresAt);
    if (user is null)
        return Results.Unauthorized();

    return TypedResults.Ok(user);
})
.WithName("data");

app.Run();

public record TokenStats(
    [property: JsonPropertyName("generated_tokens")] int GeneratedTokens,
    [property: JsonPropertyName("distinct_tokens")] int DistinctTokens);

public class User
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

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

    [JsonIgnore]
    public DateTimeOffset ExpiresAt { get; set; }
}

public class UserContainer
{
    public ConcurrentBag<User> Users { get; } = [];
}
