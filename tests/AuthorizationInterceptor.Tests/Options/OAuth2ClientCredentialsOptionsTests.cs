using AuthorizationInterceptor.Options;
using FluentAssertions;

namespace AuthorizationInterceptor.Tests.Options;

public class OAuth2ClientCredentialsOptionsTests
{
    [Fact]
    public void DefaultsShouldUseClientSecretBasic()
    {
        var options = new OAuth2ClientCredentialsOptions();

        options.TokenEndpoint.Should().BeNull();
        options.ClientId.Should().BeEmpty();
        options.ClientSecret.Should().BeEmpty();
        options.ClientAuthenticationMethod.Should().Be(OAuth2ClientAuthenticationMethod.ClientSecretBasic);
        options.Scopes.Should().BeEmpty();
        options.AdditionalParameters.Should().BeEmpty();
    }

    [Theory]
    [InlineData(OAuth2ClientAuthenticationMethod.ClientSecretBasic)]
    [InlineData(OAuth2ClientAuthenticationMethod.ClientSecretPost)]
    public void ValidateWithValidConfigurationShouldNotThrow(OAuth2ClientAuthenticationMethod authenticationMethod)
    {
        var options = ValidOptions();
        options.ClientAuthenticationMethod = authenticationMethod;
        options.Scopes.Add("api.read");
        options.Scopes.Add("api.write");
        options.AdditionalParameters.Add("audience", "https://api.example.com");
        options.AdditionalParameters.Add("resource", "https://resource.example.com");

        var exception = Record.Exception(options.Validate);

        exception.Should().BeNull();
    }

    [Fact]
    public void ValidateWithoutOptionalScopesOrAdditionalParametersShouldNotThrow()
    {
        var options = ValidOptions();

        var exception = Record.Exception(options.Validate);

        exception.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/connect/token")]
    public void ValidateWhenTokenEndpointIsMissingOrRelativeShouldThrow(string? endpoint)
    {
        var options = ValidOptions();
        options.TokenEndpoint = endpoint is null ? null : new Uri(endpoint, UriKind.Relative);

        var exception = options.Validate.Should().ThrowExactly<ArgumentException>();

        exception.ParamName.Should().Be(nameof(options.TokenEndpoint));
        exception.Message.Should().Contain("absolute URI");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void ValidateWhenClientIdIsMissingShouldThrow(string? clientId)
    {
        var options = ValidOptions();
        options.ClientId = clientId!;

        var exception = options.Validate.Should().ThrowExactly<ArgumentException>();

        exception.ParamName.Should().Be(nameof(options.ClientId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void ValidateWhenClientSecretIsMissingShouldThrow(string? clientSecret)
    {
        var options = ValidOptions();
        options.ClientSecret = clientSecret!;

        var exception = options.Validate.Should().ThrowExactly<ArgumentException>();

        exception.ParamName.Should().Be(nameof(options.ClientSecret));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void ValidateWhenClientAuthenticationMethodIsUndefinedShouldThrow(int authenticationMethod)
    {
        var options = ValidOptions();
        options.ClientAuthenticationMethod = (OAuth2ClientAuthenticationMethod)authenticationMethod;

        var exception = options.Validate.Should().ThrowExactly<ArgumentOutOfRangeException>();

        exception.ParamName.Should().Be(nameof(options.ClientAuthenticationMethod));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void ValidateWhenScopesContainsMissingValueShouldThrow(string? scope)
    {
        var options = ValidOptions();
        options.Scopes.Add("api.read");
        options.Scopes.Add(scope!);

        var exception = options.Validate.Should().ThrowExactly<ArgumentException>();

        exception.ParamName.Should().Be(nameof(options.Scopes));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void ValidateWhenAdditionalParameterKeyIsEmptyOrWhitespaceShouldThrow(string key)
    {
        var options = ValidOptions();
        options.AdditionalParameters.Add("audience", "https://api.example.com");
        options.AdditionalParameters.Add(key, "value");

        var exception = options.Validate.Should().ThrowExactly<ArgumentException>();

        exception.ParamName.Should().Be(nameof(options.AdditionalParameters));
    }

    [Theory]
    [InlineData("grant_type")]
    [InlineData("client_id")]
    [InlineData("client_secret")]
    [InlineData("scope")]
    public void ValidateWhenAdditionalParameterIsManagedByHandlerShouldThrow(string parameter)
    {
        var options = ValidOptions();
        options.AdditionalParameters.Add(parameter, "value");

        var exception = options.Validate.Should().ThrowExactly<ArgumentException>();

        exception.ParamName.Should().Be(nameof(options.AdditionalParameters));
        exception.Message.Should().Contain(parameter);
    }

    [Theory]
    [InlineData("http://identity.example.com/token")]
    [InlineData("ftp://identity.example.com/token")]
    public void ValidateWithNonHttpsRemoteEndpointShouldThrow(string endpoint)
    {
        var options = ValidOptions();
        options.TokenEndpoint = new Uri(endpoint);

        var exception = options.Validate.Should().ThrowExactly<ArgumentException>();

        exception.ParamName.Should().Be(nameof(options.TokenEndpoint));
        exception.Message.Should().Contain("HTTPS");
    }

    [Theory]
    [InlineData("http://localhost:5000/token")]
    [InlineData("http://127.0.0.1:5000/token")]
    [InlineData("http://[::1]:5000/token")]
    public void ValidateWithHttpLoopbackEndpointShouldNotThrow(string endpoint)
    {
        var options = ValidOptions();
        options.TokenEndpoint = new Uri(endpoint);

        var exception = Record.Exception(options.Validate);

        exception.Should().BeNull();
    }

    private static OAuth2ClientCredentialsOptions ValidOptions()
        => new()
        {
            TokenEndpoint = new Uri("https://identity.example.com/token"),
            ClientId = "client",
            ClientSecret = "secret"
        };
}
