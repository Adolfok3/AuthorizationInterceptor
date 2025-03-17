using AuthorizationInterceptor.Extensions.Abstractions.Headers;

namespace AuthorizationInterceptor.Extensions.Abstractions.Tests.Headers;

public class AuthorizationHeadersTests
{
    [Fact]
    public void CreateAuthorizationHeaders_WithDefaultProperties_ShouldContainsCorrectInformation()
    {
        //Arrange
        var headers = new AuthorizationHeaders();

        //Assert
        Assert.Null(headers.ExpiresIn);
        Assert.Null(headers.OAuthHeaders);
        Assert.Empty(headers);
        Assert.NotEqual(DateTimeOffset.MinValue, headers.AuthenticatedAt);
    }
    
    [Fact]
    public void IsHeadersValid_WithNullValues_ShouldReturnTrue()
    {
        //Arrange
        var headers = new AuthorizationHeaders();
        AuthorizationHeaders headersWithOauth = new OAuthHeaders("test", "test");

        //Act & Assert
        Assert.True(headers.IsHeadersValid());
        Assert.True(headersWithOauth.IsHeadersValid());
    }
    
    [Fact]
    public void IsHeadersValid_WithValues_ShouldReturnTrue()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("test", "test", 100);

        //Act & Assert
        Assert.True(headers.IsHeadersValid());
    }
    
    [Fact]
    public async Task IsHeadersValid_WithValues_ShouldReturnFalse()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("test", "test", 1);

        //Act
        await Task.Delay(2);
        
        //Assert
        Assert.True(headers.IsHeadersValid());
    }
    
    [Fact]
    public async Task GetRealExpiration_ShouldReturnExpired()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromSeconds(1));
        
        //Act
        await Task.Delay(2);
        
        //Assert
        Assert.Null(headers.OAuthHeaders);
        Assert.Empty(headers);
        Assert.NotEqual(DateTimeOffset.MinValue, headers.AuthenticatedAt);
        Assert.NotEqual(TimeSpan.Zero, headers.GetRealExpiration());
    }
    
    [Fact]
    public void GetRealExpiration_ShouldReturnValid()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromSeconds(10));
        
        //Act & Assert
        Assert.Null(headers.OAuthHeaders);
        Assert.Empty(headers);
        Assert.NotEqual(DateTimeOffset.MinValue, headers.AuthenticatedAt);
        Assert.NotEqual(TimeSpan.FromSeconds(10), headers.GetRealExpiration());
    }

    [Fact]
    public void CreateAuthorizationHeaders_WithProperties_ShouldContainsCorrectInformation()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Test", "Test" }
        };

        //Assert
        Assert.NotNull(headers.ExpiresIn);
        Assert.NotEmpty(headers);
        Assert.NotEqual(headers.ExpiresIn, headers.GetRealExpiration());
        Assert.Contains(headers, a => a.Key == "Test" && a.Value == "Test");
        Assert.NotEqual(DateTimeOffset.MinValue, headers.AuthenticatedAt);
    }

    [Fact]
    public void CreateAuthorizationHeaders_FromOAuth_WithCorrectProperties_ShouldCreateCorrectly()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("Token", "Bearer", 3000, "refreskToken", 5000);

        //Assert
        Assert.NotNull(headers.ExpiresIn);
        Assert.Equal(TimeSpan.FromSeconds(5000), headers.ExpiresIn);
        Assert.NotEmpty(headers);
        Assert.Contains(headers, a => a.Key == "Authorization" && a.Value == "Bearer Token");
        Assert.NotEqual(DateTimeOffset.MinValue, headers.AuthenticatedAt);
        Assert.NotNull(headers.OAuthHeaders);
        Assert.Equal("Token", headers.OAuthHeaders.AccessToken);
        Assert.Equal("Bearer", headers.OAuthHeaders.TokenType);
        Assert.Equal(3000, headers.OAuthHeaders.ExpiresIn);
        Assert.Equal("refreskToken", headers.OAuthHeaders.RefreshToken);
        Assert.Equal(5000, headers.OAuthHeaders.ExpiresInRefreshToken);
    }

    [Fact]
    public void CreateAuthorizationHeaders_FromOAuth_WithCustomProperties_ShouldCreateCorrectly()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("Token", "Bearer");

        //Assert
        Assert.Null(headers.ExpiresIn);
        Assert.NotEmpty(headers);
        Assert.Contains(headers, a => a.Key == "Authorization" && a.Value == "Bearer Token");
        Assert.NotEqual(DateTimeOffset.MinValue, headers.AuthenticatedAt);
    }

    [Fact]
    public void CreateAuthorizationHeaders_FromOAuth_WithWrongProperties_ShouldNotCreate()
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
        Assert.Equal("Property 'TokenType' is required.", Assert.Throws<ArgumentException>(act1).Message);
        Assert.Equal("Property 'AccessToken' is required.", Assert.Throws<ArgumentException>(act2).Message);
        Assert.Equal("ExpiresIn must be greater tha 0.", Assert.Throws<ArgumentException>(act3).Message);
        Assert.Equal("ExpiresInRefreshToken must be greater than 0 if RefreshToken is provided.", Assert.Throws<ArgumentException>(act4).Message);
    }
}
