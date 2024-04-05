using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Json;
using System.Globalization;
using System.Text.Json;

namespace AuthorizationInterceptor.Tests.Json;

public class AuthorizationEntryJsonSerializerTests
{
    [Fact]
    public void Serialiaze_WithEmptyProperties_ShouldSerialize()
    {
        //Arrange
        var entry = new AuthorizationEntry();

        //Act
        var json = AuthorizationEntryJsonSerializer.Serialize(entry);

        //Assert
        Assert.NotEmpty(json);
        Assert.Contains(@"""Headers"":{}", json);
        Assert.Contains(@"""ExpiresIn"":null", json);
        Assert.Contains(@"""OAuthEntry"":null", json);
        Assert.Contains(@$"""AuthenticatedAt"":""{DateTimeOffset.UtcNow.Date:yyyy-MM-dd}", json);
    }

    [Fact]
    public void Serialiaze_WithProperties_ShouldSerialize()
    {
        //Arrange
        var entry = new AuthorizationEntry(TimeSpan.FromMinutes(3))
        {
            { "Header1", "Value1" },
            { "Header2", "Value2" }
        };

        //Act
        var json = AuthorizationEntryJsonSerializer.Serialize(entry);

        //Assert
        Assert.NotEmpty(json);
        Assert.Contains(@"""Headers"":{""Header1"":""Value1"",""Header2"":""Value2""}", json);
        Assert.Contains(@"""ExpiresIn"":""00:03:00""", json);
        Assert.Contains(@"""OAuthEntry"":null", json);
        Assert.Contains(@$"""AuthenticatedAt"":""{DateTimeOffset.UtcNow.Date:yyyy-MM-dd}", json);
    }

    [Fact]
    public void Serialiaze_WithOAuthProperties_ShouldSerialize()
    {
        //Arrange
        AuthorizationEntry entry = new OAuthEntry("AccessToken", "TokenType", 12345, "RefreshToken", "Scope");

        //Act
        var json = AuthorizationEntryJsonSerializer.Serialize(entry);

        //Assert
        Assert.NotEmpty(json);
        Assert.Contains(@"""Headers"":{""Authorization"":""TokenType AccessToken""}", json);
        Assert.Contains(@"""ExpiresIn"":""03:25:45""", json);
        Assert.Contains(@"""OAuthEntry"":{""AccessToken"":""AccessToken"",""TokenType"":""TokenType"",""ExpiresIn"":""12345"",""RefreshToken"":""RefreshToken"",""Scope"":""Scope""}", json);
        Assert.Contains(@$"""AuthenticatedAt"":""{DateTimeOffset.UtcNow.Date:yyyy-MM-dd}", json);
    }

    [Fact]
    public void Deserialiaze_WithEmptyProperties_ShouldDeserialize()
    {
        //Arrange
        var json = @"{}";

        //Act
        var entry = AuthorizationEntryJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(entry);
        Assert.Empty(entry);
        Assert.Null(entry.OAuthEntry);
        Assert.Null(entry.ExpiresIn);
        Assert.Equal(DateTimeOffset.MinValue, entry.AuthenticatedAt);
    }

    [Fact]
    public void Deserialiaze_WithPropertiesEmpty_ShouldDeserialize()
    {
        //Arrange
        var json = @"{
                        ""Headers"":{},
                        ""OAuthEntry"":null,
                        ""ExpiresIn"":null
                     }";

        //Act
        var entry = AuthorizationEntryJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(entry);
        Assert.Empty(entry);
        Assert.Null(entry.OAuthEntry);
        Assert.Null(entry.ExpiresIn);
        Assert.Equal(DateTimeOffset.MinValue, entry.AuthenticatedAt);
    }

    [Fact]
    public void Deserialiaze_WithPropertiesEmptyOAuth_ShouldDeserialize()
    {
        //Arrange
        var json = @"{
                        ""Headers"":{},
                        ""OAuthEntry"":{""AccessToken"":null},
                        ""ExpiresIn"":null
                     }";

        //Act
        var entry = AuthorizationEntryJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(entry);
        Assert.Empty(entry);
        Assert.Null(entry.OAuthEntry);
        Assert.Null(entry.ExpiresIn);
        Assert.Equal(DateTimeOffset.MinValue, entry.AuthenticatedAt);
    }

    [Fact]
    public void Deserialiaze_WithPropertiesAndEmptyOAuth_ShouldDeserialize()
    {
        //Arrange
        var json = @"{
                        ""Headers"":{
                            ""Header1"": ""Value1""
                        },
                        ""OAuthEntry"":{""AccessToken"":""AccessToken"", ""TokenType"":""""},
                        ""ExpiresIn"":""00:03:00""
                     }";

        //Act
        var entry = AuthorizationEntryJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(entry);
        Assert.NotEmpty(entry);
        Assert.Contains(entry, a => a.Key == "Header1" && a.Value == "Value1");
        Assert.Null(entry.OAuthEntry);
        Assert.NotNull(entry.ExpiresIn);
        Assert.Equal(TimeSpan.FromMinutes(3), entry.ExpiresIn.Value);
        Assert.Equal(DateTimeOffset.MinValue, entry.AuthenticatedAt);
    }

    [Fact]
    public void Deserialiaze_WithProperties_ShouldDeserialize()
    {
        //Arrange
        var json = @"{
                        ""Headers"":{
                            ""Header1"": ""Value1""
                        },
                        ""OAuthEntry"":{""AccessToken"":""AccessToken"", ""TokenType"":""Basic"", ""ExpiresIn"":""120""},
                        ""ExpiresIn"":""00:03:00"",
                        ""AuthenticatedAt"": ""2024-04-05T19:47:35.1564179+00:00""
                     }";

        //Act
        var entry = AuthorizationEntryJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(entry);
        Assert.NotEmpty(entry);
        Assert.Contains(entry, a => a.Key == "Header1" && a.Value == "Value1");
        Assert.NotNull(entry.OAuthEntry);
        Assert.Equal("AccessToken", entry.OAuthEntry.AccessToken);
        Assert.Equal("Basic", entry.OAuthEntry.TokenType);
        Assert.NotNull(entry.OAuthEntry.ExpiresIn);
        Assert.Equal(120, entry.OAuthEntry.ExpiresIn.Value);
        Assert.Null(entry.OAuthEntry.RefreshToken);
        Assert.Null(entry.OAuthEntry.Scope);
        Assert.NotNull(entry.ExpiresIn);
        Assert.Equal(TimeSpan.FromMinutes(3), entry.ExpiresIn.Value);
        Assert.Equal(@"""2024-04-05T19:47:35.1564179+00:00""", JsonSerializer.Serialize(entry.AuthenticatedAt));
    }
}
