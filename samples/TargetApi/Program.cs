using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddSingleton(new UserContainer());

var app = builder.Build();
app.UseOpenApi();
app.UseSwaggerUi();

app.MapPost("/auth", (UserContainer users, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("TargetApi");
    logger.LogDebug("Received request on /auth endpoint");
    var user = new User
    {
        AccessToken = Guid.NewGuid().ToString(),
        RefreshToken = Guid.NewGuid().ToString(),
        TokenType = "Bearer",
        ExpiresIn = 30,
        RefreshTokenExpiresIn = 60,
        ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30),
    };

    users.Users.Add(user);
    return user;
})
.WithName("auth");

app.MapPost("/refresh", (UserContainer users, [FromQuery] string refresh, ILoggerFactory loggerFactory) =>
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
        AccessToken = Guid.NewGuid().ToString(),
        RefreshToken = Guid.NewGuid().ToString(),
        TokenType = "Bearer",
        ExpiresIn = 30,
        RefreshTokenExpiresIn = 60,
        ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30)
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
    if (!users.Users.Any(a => a.AccessToken == token && DateTimeOffset.UtcNow < a.ExpiresAt))
        return Results.Unauthorized();

    return Results.Ok();
})
.WithName("data");


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

    [JsonIgnore]
    public DateTimeOffset ExpiresAt { get; set; }
}

public class UserContainer
{
    public List<User> Users { get; set; } = [];
}
