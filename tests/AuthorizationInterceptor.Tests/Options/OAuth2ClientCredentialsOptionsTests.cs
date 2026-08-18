using AuthorizationInterceptor.Options;

namespace AuthorizationInterceptor.Tests.Options;

public class OAuth2ClientCredentialsOptionsTests
{
    [Fact]
    public void Defaults_ShouldUseClientSecretBasic()
    {
        var options = new OAuth2ClientCredentialsOptions();

        Assert.Equal(OAuth2ClientAuthenticationMethod.ClientSecretBasic, options.ClientAuthenticationMethod);
        Assert.Empty(options.Scopes);
        Assert.Empty(options.AdditionalParameters);
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        var options = ValidOptions();

        var exception = Record.Exception(options.Validate);

        Assert.Null(exception);
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

        Assert.Contains(parameter, exception.Message);
    }

    [Fact]
    public void Validate_WithNonHttpsRemoteEndpoint_ShouldThrow()
    {
        var options = ValidOptions();
        options.TokenEndpoint = new Uri("http://identity.example.com/token");

        var exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Contains(nameof(options.TokenEndpoint), exception.Message);
    }

    private static OAuth2ClientCredentialsOptions ValidOptions()
        => new()
        {
            TokenEndpoint = new Uri("https://identity.example.com/token"),
            ClientId = "client",
            ClientSecret = "secret"
        };
}
