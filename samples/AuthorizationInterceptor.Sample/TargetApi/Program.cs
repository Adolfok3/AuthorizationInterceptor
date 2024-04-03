using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(new UserContainer());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/auth", (UserContainer users) =>
{
    var user = new User
    {
        AccessToken = Guid.NewGuid().ToString(),
        RefreshToken = Guid.NewGuid().ToString(),
        TokenType = "Bearer",
        ExpiresIn = 30,
        ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30),
    };

    users.Users.Add(user);
    return user;
})
.WithName("auth")
.WithOpenApi();

app.MapPost("/refresh", (UserContainer users, [FromQuery] string refresh) =>
{
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
        ExpiresIn = 60,
        ExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30),
    };

    users.Users.Add(user);
    return Results.Ok(user);
})
.WithName("refresh")
.WithOpenApi();

app.MapGet("/data", (HttpRequest request, UserContainer users, [FromHeader(Name = "Authorization")] string? token = null) =>
{
    if (string.IsNullOrWhiteSpace(token))
        return Results.Unauthorized();

    token = token.Replace("Bearer ", string.Empty);
    if (!users.Users.Any(a => a.AccessToken == token && DateTimeOffset.UtcNow < a.ExpiresAt))
        return Results.Unauthorized();

    return Results.Ok();
})
.WithName("data")
.WithOpenApi();


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

    [JsonIgnore]
    public DateTimeOffset ExpiresAt { get; set; }
}

public class UserContainer
{
    public List<User> Users { get; set; } = [];
}
