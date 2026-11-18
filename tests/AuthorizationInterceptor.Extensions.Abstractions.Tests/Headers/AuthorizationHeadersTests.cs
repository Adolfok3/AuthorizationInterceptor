using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using FluentAssertions;

namespace AuthorizationInterceptor.Extensions.Abstractions.Tests.Headers;

public class AuthorizationHeadersTests
{
    [Fact]
    public void CreateAuthorizationHeadersWithDefaultPropertiesShouldContainsCorrectInformation()
    {
        //Arrange
        var headers = new AuthorizationHeaders();

        //Assert
        headers.ExpiresIn.Should().BeNull();
        headers.OAuthHeaders.Should().BeNull();
        headers.Should().BeEmpty();
        headers.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
    }

    [Fact]
    public void IsHeadersValidWithNullValuesShouldReturnTrue()
    {
        //Arrange
        var headers = new AuthorizationHeaders();
        AuthorizationHeaders headersWithOauth = new OAuthHeaders("test", "test");

        //Act & Assert
        headers.IsHeadersValid().Should().BeTrue();
        headersWithOauth.IsHeadersValid().Should().BeTrue();
    }

    [Fact]
    public void IsHeadersValidWithValuesShouldReturnTrue()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("test", "test", 100);

        //Act & Assert
        headers.IsHeadersValid().Should().BeTrue();
    }

    [Fact]
    public async Task IsHeadersValidWithValuesShouldReturnFalse()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("test", "test", 1);

        //Act
        await Task.Delay(2);

        //Assert
        headers.IsHeadersValid().Should().BeTrue();
    }

    [Fact]
    public async Task GetRealExpirationShouldReturnExpired()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromSeconds(1));

        //Act
        await Task.Delay(2);

        //Assert
        headers.OAuthHeaders.Should().BeNull();
        headers.Should().BeEmpty();
        headers.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
        headers.GetRealExpiration().Should().NotBe(TimeSpan.Zero);
    }

    [Fact]
    public void GetRealExpirationShouldReturnValid()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromSeconds(10));

        //Act & Assert
        headers.OAuthHeaders.Should().BeNull();
        headers.Should().BeEmpty();
        headers.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
        headers.GetRealExpiration().Should().NotBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CreateAuthorizationHeadersWithPropertiesShouldContainsCorrectInformation()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Test", "Test" }
        };

        //Assert
        headers.ExpiresIn.Should().NotBeNull();
        headers.Should().NotBeEmpty();
        headers.GetRealExpiration().Should().NotBe(headers.ExpiresIn.Value);
        headers.Should().Contain(a => a.Key == "Test" && a.Value == "Test");
        headers.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
    }

    [Fact]
    public void CreateAuthorizationHeadersFromOAuthWithCorrectPropertiesShouldCreateCorrectly()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("Token", "Bearer", 3000, "refreskToken", 5000);

        //Assert
        headers.ExpiresIn.Should().NotBeNull();
        headers.ExpiresIn.Should().Be(TimeSpan.FromSeconds(5000));
        headers.Should().NotBeEmpty();
        headers.Should().Contain(a => a.Key == "Authorization" && a.Value == "Bearer Token");
        headers.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
        headers.OAuthHeaders.Should().NotBeNull();
        headers.OAuthHeaders.AccessToken.Should().Be("Token");
        headers.OAuthHeaders.TokenType.Should().Be("Bearer");
        headers.OAuthHeaders.ExpiresIn.Should().Be(3000);
        headers.OAuthHeaders.RefreshToken.Should().Be("refreskToken");
        headers.OAuthHeaders.ExpiresInRefreshToken.Should().Be(5000);
    }

    [Fact]
    public void CreateAuthorizationHeadersFromOAuthWithCustomPropertiesShouldCreateCorrectly()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("Token", "Bearer");

        //Assert
        headers.ExpiresIn.Should().BeNull();
        headers.Should().NotBeEmpty();
        headers.Should().Contain(a => a.Key == "Authorization" && a.Value == "Bearer Token");
        headers.AuthenticatedAt.Should().NotBe(DateTimeOffset.MinValue);
    }

    [Fact]
    public void CreateAuthorizationHeadersFromOAuthWithWrongPropertiesShouldNotCreate()
    {
        //Arrange
        var entryWithToken = new OAuthHeaders("test", "");
        var entryWithType = new OAuthHeaders("", "test");
        var entryExpires = new OAuthHeaders("test", "test", -30000);
        var entryWithRefreshTokenWithoutExpiration = new OAuthHeaders("test", "test", 30000, "refreshtoken", null);

        //Act
        var act1 = () => (AuthorizationHeaders)entryWithToken;
        var act2 = () => (AuthorizationHeaders)entryWithType;
        var act3 = () => (AuthorizationHeaders)entryExpires;
        var act4 = () => (AuthorizationHeaders)entryWithRefreshTokenWithoutExpiration;

        //Assert
        act1.Should().ThrowExactly<ArgumentException>().WithMessage("Property 'TokenType' is required.");
        act2.Should().ThrowExactly<ArgumentException>().WithMessage("Property 'AccessToken' is required.");
        act3.Should().ThrowExactly<ArgumentException>().WithMessage("ExpiresIn must be greater tha 0.");
        act4.Should().ThrowExactly<ArgumentException>().WithMessage("ExpiresInRefreshToken must be greater than 0 if RefreshToken is provided.");
    }
}
