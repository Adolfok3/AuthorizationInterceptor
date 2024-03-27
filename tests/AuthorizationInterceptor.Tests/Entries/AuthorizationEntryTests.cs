using AuthorizationInterceptor.Entries;

namespace AuthorizationInterceptor.Tests.Entries;

public class AuthorizationEntryTests
{
    [Fact]
    public void CreateAuthorizationEntry_WithDefaultProperties_ShouldContainsCorrectInformation()
    {
        //Arrange
        var entry = new AuthorizationEntry();

        //Assert
        Assert.Null(entry.ExpiresIn);
        Assert.Empty(entry);
        Assert.NotEqual(DateTimeOffset.MinValue, entry.AuthenticatedAt);
    }

    [Fact]
    public void CreateAuthorizationEntry_WithProperties_ShouldContainsCorrectInformation()
    {
        //Arrange
        var entry = new AuthorizationEntry(TimeSpan.FromMinutes(3))
        {
            { "Test", "Test" }
        };

        //Assert
        Assert.NotNull(entry.ExpiresIn);
        Assert.NotEmpty(entry);
        Assert.Contains(entry, a => a.Key == "Test" && a.Value == "Test");
        Assert.NotEqual(DateTimeOffset.MinValue, entry.AuthenticatedAt);
    }

    [Fact]
    public void CreateAuthorizationEntry_FromOAuth_WithCorrectProperties_ShouldCreateCorrectly()
    {
        //Arrange
        AuthorizationEntry entry = new OAuthEntry("Token", "Bearer", 3000, "refreskToken", "client_credentials");

        //Assert
        Assert.NotNull(entry.ExpiresIn);
        Assert.Equal(TimeSpan.FromSeconds(3000), entry.ExpiresIn);
        Assert.NotEmpty(entry);
        Assert.Contains(entry, a => a.Key == "Authorization" && a.Value == "Bearer Token");
        Assert.NotEqual(DateTimeOffset.MinValue, entry.AuthenticatedAt);
        Assert.NotNull(entry.OAuthEntry);
        Assert.Equal("Token", entry.OAuthEntry.AccessToken);
        Assert.Equal("Bearer", entry.OAuthEntry.TokenType);
        Assert.Equal(3000, entry.OAuthEntry.ExpiresIn);
        Assert.Equal("refreskToken", entry.OAuthEntry.RefreshToken);
        Assert.Equal("client_credentials", entry.OAuthEntry.Scope);
    }

    [Fact]
    public void CreateAuthorizationEntry_FromOAuth_WithCustomProperties_ShouldCreateCorrectly()
    {
        //Arrange
        AuthorizationEntry entry = new OAuthEntry("Token", "Bearer");

        //Assert
        Assert.Null(entry.ExpiresIn);
        Assert.NotEmpty(entry);
        Assert.Contains(entry, a => a.Key == "Authorization" && a.Value == "Bearer Token");
        Assert.NotEqual(DateTimeOffset.MinValue, entry.AuthenticatedAt);
    }

    [Fact]
    public void CreateAuthorizationEntry_FromOAuth_WithWrongProperties_ShouldNotCreate()
    {
        //Arrange
        var entryWithToken = new OAuthEntry("test", "");
        var entryWithType = new OAuthEntry("", "test");
        var entryExpires = new OAuthEntry("test", "test", -30000);

        //Act
        var act1 = () => (AuthorizationEntry)entryWithToken;
        var act2 = () => (AuthorizationEntry)entryWithType;
        var act3 = () => (AuthorizationEntry)entryExpires;

        //Assert
        Assert.Equal("Property 'TokenType' is required.", Assert.Throws<ArgumentException>(act1).Message);
        Assert.Equal("Property 'AccessToken' is required.", Assert.Throws<ArgumentException>(act2).Message);
        Assert.Equal("ExpiresIn must be greater tha 0.", Assert.Throws<ArgumentException>(act3).Message);
    }
}
