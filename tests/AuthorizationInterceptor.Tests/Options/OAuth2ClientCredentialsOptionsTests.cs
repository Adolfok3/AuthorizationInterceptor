using AuthorizationInterceptor.Options;

namespace AuthorizationInterceptor.Tests.Options;

public class OAuth2ClientCredentialsOptionsTests
{
    [Fact]
    public void Defaults_ShouldUseClientSecretBasic()
    {
        var options = new OAuth2ClientCredentialsOptions();

        Assert.Null(options.TokenEndpoint);
        Assert.Empty(options.ClientId);
        Assert.Empty(options.ClientSecret);
        Assert.Equal(OAuth2ClientAuthenticationMethod.ClientSecretBasic, options.ClientAuthenticationMethod);
        Assert.Empty(options.Scopes);
        Assert.Empty(options.AdditionalParameters);
    }

    [Theory]
    [InlineData(OAuth2ClientAuthenticationMethod.ClientSecretBasic)]
    [InlineData(OAuth2ClientAuthenticationMethod.ClientSecretPost)]
    public void Validate_WithValidConfiguration_ShouldNotThrow(OAuth2ClientAuthenticationMethod authenticationMethod)
    {
        var options = ValidOptions();
        options.ClientAuthenticationMethod = authenticationMethod;
        options.Scopes.Add("api.read");
        options.Scopes.Add("api.write");
        options.AdditionalParameters.Add("audience", "https://api.example.com");
        options.AdditionalParameters.Add("resource", "https://resource.example.com");

        var exception = Record.Exception(options.Validate);

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithoutOptionalScopesOrAdditionalParameters_ShouldNotThrow()
    {
        var options = ValidOptions();

        var exception = Record.Exception(options.Validate);

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/connect/token")]
    public void Validate_WhenTokenEndpointIsMissingOrRelative_ShouldThrow(string? endpoint)
    {
        var options = ValidOptions();
        options.TokenEndpoint = endpoint is null ? null : new Uri(endpoint, UriKind.Relative);

        var exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(options.TokenEndpoint), exception.ParamName);
        Assert.Contains("absolute URI", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void Validate_WhenClientIdIsMissing_ShouldThrow(string? clientId)
    {
        var options = ValidOptions();
        options.ClientId = clientId!;

        var exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(options.ClientId), exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void Validate_WhenClientSecretIsMissing_ShouldThrow(string? clientSecret)
    {
        var options = ValidOptions();
        options.ClientSecret = clientSecret!;

        var exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(options.ClientSecret), exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Validate_WhenClientAuthenticationMethodIsUndefined_ShouldThrow(int authenticationMethod)
    {
        var options = ValidOptions();
        options.ClientAuthenticationMethod = (OAuth2ClientAuthenticationMethod)authenticationMethod;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        Assert.Equal(nameof(options.ClientAuthenticationMethod), exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void Validate_WhenScopesContainsMissingValue_ShouldThrow(string? scope)
    {
        var options = ValidOptions();
        options.Scopes.Add("api.read");
        options.Scopes.Add(scope!);

        var exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(options.Scopes), exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void Validate_WhenAdditionalParameterKeyIsEmptyOrWhitespace_ShouldThrow(string key)
    {
        var options = ValidOptions();
        options.AdditionalParameters.Add("audience", "https://api.example.com");
        options.AdditionalParameters.Add(key, "value");

        var exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(options.AdditionalParameters), exception.ParamName);
    }

    [Theory]
    [InlineData("grant_type")]
    [InlineData("client_id")]
    [InlineData("client_secret")]
    [InlineData("scope")]
    public void Validate_WhenAdditionalParameterIsManagedByHandler_ShouldThrow(string parameter)
    {
        var options = ValidOptions();
        options.AdditionalParameters.Add(parameter, "value");

        var exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(options.AdditionalParameters), exception.ParamName);
        Assert.Contains(parameter, exception.Message);
    }

    [Theory]
    [InlineData("http://identity.example.com/token")]
    [InlineData("ftp://identity.example.com/token")]
    public void Validate_WithNonHttpsRemoteEndpoint_ShouldThrow(string endpoint)
    {
        var options = ValidOptions();
        options.TokenEndpoint = new Uri(endpoint);

        var exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Equal(nameof(options.TokenEndpoint), exception.ParamName);
        Assert.Contains("HTTPS", exception.Message);
    }

    [Theory]
    [InlineData("http://localhost:5000/token")]
    [InlineData("http://127.0.0.1:5000/token")]
    [InlineData("http://[::1]:5000/token")]
    public void Validate_WithHttpLoopbackEndpoint_ShouldNotThrow(string endpoint)
    {
        var options = ValidOptions();
        options.TokenEndpoint = new Uri(endpoint);

        var exception = Record.Exception(options.Validate);

        Assert.Null(exception);
    }

    private static OAuth2ClientCredentialsOptions ValidOptions()
        => new()
        {
            TokenEndpoint = new Uri("https://identity.example.com/token"),
            ClientId = "client",
            ClientSecret = "secret"
        };
}
