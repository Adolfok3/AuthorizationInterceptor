using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Json;
using System.Text.Json;

namespace AuthorizationInterceptor.Extensions.Abstractions.Tests.Json;

public class AuthorizationHeadersJsonSerializerTests
{
    [Fact]
    public void Serialiaze_WithEmptyProperties_ShouldSerialize()
    {
        //Arrange
        var headers = new AuthorizationHeaders();

        //Act
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);

        //Assert
        Assert.NotEmpty(json);
        Assert.Contains(@"""Headers"":{}", json);
        Assert.Contains(@"""ExpiresIn"":null", json);
        Assert.Contains(@"""OAuthHeaders"":null", json);
        Assert.Contains(@$"""AuthenticatedAt"":""{DateTimeOffset.UtcNow.Date:yyyy-MM-dd}", json);
    }

    [Fact]
    public void Serialiaze_WithProperties_ShouldSerialize()
    {
        //Arrange
        var headers = new AuthorizationHeaders(TimeSpan.FromMinutes(3))
        {
            { "Header1", "Value1" },
            { "Header2", "Value2" }
        };

        //Act
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);

        //Assert
        Assert.NotEmpty(json);
        Assert.Contains(@"""Headers"":{""Header1"":""Value1"",""Header2"":""Value2""}", json);
        Assert.Contains(@"""ExpiresIn"":""00:03:00""", json);
        Assert.Contains(@"""OAuthHeaders"":null", json);
        Assert.Contains(@$"""AuthenticatedAt"":""{DateTimeOffset.UtcNow.Date:yyyy-MM-dd}", json);
    }

    [Fact]
    public void Serialiaze_WithOAuthProperties_ShouldSerialize()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("AccessToken", "TokenType", 12345, "RefreshToken", 54321);

        //Act
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);

        //Assert
        Assert.NotEmpty(json);
        Assert.Contains(@"""Headers"":{""Authorization"":""TokenType AccessToken""}", json);
        Assert.Contains(@"""ExpiresIn"":""15:05:21""", json);
        Assert.Contains(@"""OAuthHeaders"":{""AccessToken"":""AccessToken"",""TokenType"":""TokenType"",""ExpiresIn"":""12345"",""RefreshToken"":""RefreshToken"",""ExpiresInRefreshToken"":""54321""}", json);
        Assert.Contains(@$"""AuthenticatedAt"":""{DateTimeOffset.UtcNow.Date:yyyy-MM-dd}", json);
    }

    [Fact]
    public void Deserialiaze_WithEmptyProperties_ShouldDeserialize()
    {
        //Arrange
        var json = @"{}";

        //Act
        var headers = AuthorizationHeadersJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(headers);
        Assert.Empty(headers);
        Assert.Null(headers.OAuthHeaders);
        Assert.Null(headers.ExpiresIn);
        Assert.Equal(DateTimeOffset.MinValue, headers.AuthenticatedAt);
    }

    [Fact]
    public void Deserialiaze_WithPropertiesEmpty_ShouldDeserialize()
    {
        //Arrange
        var json = @"{
                        ""Headers"":{},
                        ""OAuthHeaders"":null,
                        ""ExpiresIn"":null
                     }";

        //Act
        var headers = AuthorizationHeadersJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(headers);
        Assert.Empty(headers);
        Assert.Null(headers.OAuthHeaders);
        Assert.Null(headers.ExpiresIn);
        Assert.Equal(DateTimeOffset.MinValue, headers.AuthenticatedAt);
    }

    [Fact]
    public void Deserialiaze_WithPropertiesEmptyOAuth_ShouldDeserialize()
    {
        //Arrange
        var json = @"{
                        ""Headers"":{},
                        ""OAuthHeaders"":{""AccessToken"":null},
                        ""ExpiresIn"":null
                     }";

        //Act
        var headers = AuthorizationHeadersJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(headers);
        Assert.Empty(headers);
        Assert.Null(headers.OAuthHeaders);
        Assert.Null(headers.ExpiresIn);
        Assert.Equal(DateTimeOffset.MinValue, headers.AuthenticatedAt);
    }

    [Fact]
    public void Deserialiaze_WithPropertiesAndEmptyOAuth_ShouldDeserialize()
    {
        //Arrange
        var json = @"{
                        ""Headers"":{
                            ""Header1"": ""Value1""
                        },
                        ""OAuthHeaders"":{""AccessToken"":""AccessToken"", ""TokenType"":""""},
                        ""ExpiresIn"":""00:03:00""
                     }";

        //Act
        var headers = AuthorizationHeadersJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(headers);
        Assert.NotEmpty(headers);
        Assert.Contains(headers, a => a is { Key: "Header1", Value: "Value1" });
        Assert.Null(headers.OAuthHeaders);
        Assert.NotNull(headers.ExpiresIn);
        Assert.Equal(TimeSpan.FromMinutes(3), headers.ExpiresIn.Value);
        Assert.Equal(DateTimeOffset.MinValue, headers.AuthenticatedAt);
    }

    [Fact]
    public void Deserialiaze_WithProperties_ShouldDeserialize()
    {
        //Arrange
        var json = @"{
                        ""Headers"":{
                            ""Header1"": ""Value1""
                        },
                        ""OAuthHeaders"":{""AccessToken"":""AccessToken"", ""TokenType"":""Basic"", ""ExpiresIn"":""120""},
                        ""ExpiresIn"":""00:03:00"",
                        ""AuthenticatedAt"": ""2024-04-05T19:47:35.1564179+00:00""
                     }";

        //Act
        var headers = AuthorizationHeadersJsonSerializer.Deserialize(json);

        //Assert
        Assert.NotNull(headers);
        Assert.NotEmpty(headers);
        Assert.Contains(headers, a => a is { Key: "Header1", Value: "Value1" });
        Assert.NotNull(headers.OAuthHeaders);
        Assert.Equal("AccessToken", headers.OAuthHeaders.AccessToken);
        Assert.Equal("Basic", headers.OAuthHeaders.TokenType);
        Assert.NotNull(headers.OAuthHeaders.ExpiresIn);
        Assert.Equal(120, headers.OAuthHeaders.ExpiresIn.Value);
        Assert.Null(headers.OAuthHeaders.RefreshToken);
        Assert.Null(headers.OAuthHeaders.ExpiresInRefreshToken);
        Assert.NotNull(headers.ExpiresIn);
        Assert.Equal(TimeSpan.FromMinutes(3), headers.ExpiresIn.Value);
        Assert.Equal(@"""2024-04-05T19:47:35.1564179+00:00""", JsonSerializer.Serialize(headers.AuthenticatedAt));
    }
}
