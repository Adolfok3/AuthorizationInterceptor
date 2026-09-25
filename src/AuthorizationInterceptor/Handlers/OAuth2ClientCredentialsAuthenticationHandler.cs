using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace AuthorizationInterceptor.Handlers;

internal sealed class OAuth2ClientCredentialsAuthenticationHandler(IHttpClientFactory httpClientFactory, OAuth2ClientCredentialsOptions options, string httpClientName) : IAuthenticationHandler
{
    private readonly HttpClient _client = httpClientFactory.CreateClient(httpClientName);

    public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(AuthorizationHeaders? expiredHeaders, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenEndpoint);
        request.Content = new FormUrlEncodedContent(BuildFormParameters());

        if (options.ClientAuthenticationMethod == OAuth2ClientAuthenticationMethod.ClientSecretBasic)
            request.Headers.Authorization = CreateBasicAuthorizationHeader(options.ClientId, options.ClientSecret);

        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<OAuth2ClientCredentialsTokenResponse>(cancellationToken);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException("The OAuth 2.0 token response did not contain an access_token.");

        if (string.IsNullOrWhiteSpace(token.TokenType))
            throw new InvalidOperationException("The OAuth 2.0 token response did not contain a token_type.");

        return new OAuthHeaders(token.AccessToken, token.TokenType, token.ExpiresIn);
    }

    private IEnumerable<KeyValuePair<string, string>> BuildFormParameters()
    {
        yield return new("grant_type", "client_credentials");

        if (options.ClientAuthenticationMethod == OAuth2ClientAuthenticationMethod.ClientSecretPost)
        {
            yield return new("client_id", options.ClientId);
            yield return new("client_secret", options.ClientSecret);
        }

        if (options.Scopes.Count > 0)
            yield return new("scope", string.Join(' ', options.Scopes));

        foreach (var parameter in options.AdditionalParameters)
            yield return parameter;
    }

    private static AuthenticationHeaderValue CreateBasicAuthorizationHeader(string clientId, string clientSecret)
    {
        var userName = WebUtility.UrlEncode(clientId);
        var password = WebUtility.UrlEncode(clientSecret);
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));
        return new AuthenticationHeaderValue("Basic", credentials);
    }

    private sealed record OAuth2ClientCredentialsTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("expires_in")] double? ExpiresIn);
}
