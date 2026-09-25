namespace AuthorizationInterceptor.Options;

/// <summary>
/// Configures the built-in OAuth 2.0 Client Credentials authentication handler.
/// </summary>
public sealed class OAuth2ClientCredentialsOptions
{
    private static readonly HashSet<string> ReservedParameters = new(StringComparer.Ordinal)
    {
        "grant_type",
        "client_id",
        "client_secret",
        "scope"
    };

    /// <summary>
    /// Gets or sets the OAuth 2.0 token endpoint.
    /// </summary>
    public Uri? TokenEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the client identifier issued by the authorization server.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret issued by the authorization server.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets the scopes requested at the token endpoint. They are sent as a space-delimited value.
    /// </summary>
    public ICollection<string> Scopes { get; } = [];

    /// <summary>
    /// Gets additional form parameters sent to the token endpoint, such as an audience or resource.
    /// OAuth parameters managed by this handler cannot be overridden here.
    /// </summary>
    public IDictionary<string, string> AdditionalParameters { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets how the client authenticates at the token endpoint.
    /// The default is <see cref="OAuth2ClientAuthenticationMethod.ClientSecretBasic"/>.
    /// </summary>
    public OAuth2ClientAuthenticationMethod ClientAuthenticationMethod { get; set; } = OAuth2ClientAuthenticationMethod.ClientSecretBasic;

    internal void Validate()
    {
        if (TokenEndpoint is null || !TokenEndpoint.IsAbsoluteUri)
            throw new ArgumentException($"{nameof(TokenEndpoint)} must be an absolute URI.", nameof(TokenEndpoint));

        if (!string.Equals(TokenEndpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !TokenEndpoint.IsLoopback)
            throw new ArgumentException($"{nameof(TokenEndpoint)} must use HTTPS unless it targets the local machine.", nameof(TokenEndpoint));

        if (string.IsNullOrWhiteSpace(ClientId))
            throw new ArgumentException($"{nameof(ClientId)} is required.", nameof(ClientId));

        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new ArgumentException($"{nameof(ClientSecret)} is required.", nameof(ClientSecret));

        if (!Enum.IsDefined(ClientAuthenticationMethod))
            throw new ArgumentOutOfRangeException(nameof(ClientAuthenticationMethod));

        if (Scopes.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"{nameof(Scopes)} cannot contain null, empty, or whitespace values.", nameof(Scopes));

        foreach (var parameter in AdditionalParameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Key))
                throw new ArgumentException($"{nameof(AdditionalParameters)} cannot contain a null, empty, or whitespace key.", nameof(AdditionalParameters));

            if (ReservedParameters.Contains(parameter.Key))
                throw new ArgumentException($"The OAuth parameter '{parameter.Key}' is managed by the handler and cannot be overridden.", nameof(AdditionalParameters));
        }
    }
}
