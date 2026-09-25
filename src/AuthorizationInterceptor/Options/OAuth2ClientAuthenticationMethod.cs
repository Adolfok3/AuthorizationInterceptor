namespace AuthorizationInterceptor.Options;

/// <summary>
/// Defines how an OAuth 2.0 confidential client authenticates at the token endpoint.
/// </summary>
public enum OAuth2ClientAuthenticationMethod
{
    /// <summary>
    /// Sends the client identifier and secret through the HTTP Basic authorization scheme.
    /// This is the default method defined by OAuth 2.0 for clients issued a password.
    /// </summary>
    ClientSecretBasic = 0,

    /// <summary>
    /// Sends <c>client_id</c> and <c>client_secret</c> in the form-encoded request body.
    /// Use this only when required by the authorization server.
    /// </summary>
    ClientSecretPost = 1
}
