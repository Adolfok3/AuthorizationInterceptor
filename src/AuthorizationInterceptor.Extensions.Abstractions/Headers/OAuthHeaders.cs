using System;

namespace AuthorizationInterceptor.Extensions.Abstractions.Headers
{
    /// <summary>
    /// Represents OAuth authentication headers containing details about an access token and its properties.
    /// </summary>
    /// <param name="AccessToken">The access token issued by the OAuth authorization server. This token is used in HTTP requests to access protected resources.</param>
    /// <param name="TokenType">The type of token issued. Typically, this is "bearer", indicating that the given access token should be used as a bearer token.</param>
    /// <param name="ExpiresIn">The lifetime in seconds of the access token. After this period, a new authentication will be required. Can be null if the expiration is not applicable. If you set <paramref name="ExpiresInRefreshToken"/>, it will be considered in cache expiration, enabling the retrieval of expired headers to facilitate the refresh token process.</param>
    /// <param name="RefreshToken">An optional refresh token that can be used to obtain new access tokens using the same authorization grant as described in the OAuth 2.0 specification. Can be null if not provided by the authorization server.</param>
    /// <param name="ExpiresInRefreshToken">The lifetime in seconds of the refresh token. After this period, you can no longer reuse the refresh token, and a new authentication will be required. Can be null if the <paramref name="RefreshToken"/> is also null.</param>
    public record OAuthHeaders(string AccessToken, string TokenType, double? ExpiresIn = null, string? RefreshToken = null, double? ExpiresInRefreshToken = null)
    {
        public void Validate()
        {
            ValidateValue(nameof(AccessToken), AccessToken);
            ValidateValue(nameof(TokenType), TokenType);

            if (ExpiresIn is <= 0)
                throw new ArgumentException("ExpiresIn must be greater tha 0.");

            if (!string.IsNullOrWhiteSpace(RefreshToken) && ExpiresInRefreshToken is null or <= 0)
                throw new ArgumentException("ExpiresInRefreshToken must be greater than 0 if RefreshToken is provided.");
        }

        public TimeSpan? GetRealTimeExpiration()
            => ExpiresInRefreshToken.HasValue
                ? TimeSpan.FromSeconds(ExpiresInRefreshToken.Value)
                : ExpiresIn.HasValue
                    ? TimeSpan.FromSeconds(ExpiresIn.Value)
                    : null;

        private static void ValidateValue(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"Property '{name}' is required.");
        }
    }
}
