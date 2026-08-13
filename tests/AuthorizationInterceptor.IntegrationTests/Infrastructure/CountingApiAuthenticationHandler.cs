using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.IntegrationTests.Infrastructure;

/// <summary>
/// The authentication handler exercised end-to-end: it performs a real HTTP call to the target's
/// <c>POST /auth</c> endpoint (through the named "auth" client) and maps the response to OAuth headers.
/// The lock is what prevents several of these from running for the same key.
/// </summary>
public sealed class CountingApiAuthenticationHandler(IHttpClientFactory httpClientFactory) : IAuthenticationHandler
{
    public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(AuthorizationHeaders? expiredHeaders, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("auth");

        var response = await client.PostAsync("/auth", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);

        return new OAuthHeaders(token!.AccessToken, token.TokenType, token.ExpiresIn, token.RefreshToken, token.RefreshTokenExpiresIn);
    }

    private sealed record AuthResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] double ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("refresh_token_expires_in")] double RefreshTokenExpiresIn);
}
