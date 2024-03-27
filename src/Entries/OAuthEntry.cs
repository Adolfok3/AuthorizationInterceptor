namespace AuthorizationInterceptor.Entries
{
    /// <summary>
    /// Represents an OAuth authentication entry containing details about an access token and its properties.
    /// </summary>
    /// <param name="AccessToken">The access token issued by the OAuth authorization server. This token is used in HTTP requests to access protected resources.</param>
    /// <param name="TokenType">The type of token issued. Typically, this is "bearer", indicating that the given access token should be used as a bearer token.</param>
    /// <param name="ExpiresIn">The lifetime in seconds of the access token. After this period, the token expires and should no longer be used. Can be null if the expiration is not applicable.</param>
    /// <param name="RefreshToken">An optional refresh token that can be used to obtain new access tokens using the same authorization grant as described in the OAuth 2.0 specification. Can be null if not provided by the authorization server.</param>
    /// <param name="Scope">The optional scope of the access request as described by the authorization server. This can be null if no scope was specified in the authorization request or if the server does not utilize scopes.</param>
    public record OAuthEntry(string AccessToken, string TokenType, double? ExpiresIn = null, string? RefreshToken = null, string? Scope = null);
}
