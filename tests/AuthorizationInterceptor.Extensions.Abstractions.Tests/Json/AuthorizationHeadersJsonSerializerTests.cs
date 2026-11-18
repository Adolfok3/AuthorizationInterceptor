using System.Text.Json;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Json;
using FluentAssertions;

namespace AuthorizationInterceptor.Extensions.Abstractions.Tests.Json;

public class AuthorizationHeadersJsonSerializerTests
{
    [Fact]
    public void SerialiazeWithEmptyPropertiesShouldSerialize()
    {
        //Arrange
        var headers = new AuthorizationHeaders();

        //Act
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);

        //Assert
        json.Should().NotBeEmpty();
        json.Should().Contain(@"""Headers"":{}");
        json.Should().Contain(@"""ExpiresIn"":null");
        json.Should().Contain(@"""OAuthHeaders"":null");
        json.Should().Contain(@$"""AuthenticatedAt"":""{DateTimeOffset.UtcNow.Date:yyyy-MM-dd}");
    }

    [Fact]
    public void SerialiazeWithPropertiesShouldSerialize()
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
        json.Should().NotBeEmpty();
        json.Should().Contain(@"""Headers"":{""Header1"":""Value1"",""Header2"":""Value2""}");
        json.Should().Contain(@"""ExpiresIn"":""00:03:00""");
        json.Should().Contain(@"""OAuthHeaders"":null");
        json.Should().Contain(@$"""AuthenticatedAt"":""{DateTimeOffset.UtcNow.Date:yyyy-MM-dd}");
    }

    [Fact]
    public void SerialiazeWithOAuthPropertiesShouldSerialize()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("AccessToken", "TokenType", 12345, "RefreshToken", 54321);

        //Act
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);

        //Assert
        json.Should().NotBeEmpty();
        json.Should().Contain(@"""Headers"":{""Authorization"":""TokenType AccessToken""}");
        json.Should().Contain(@"""ExpiresIn"":""15:05:21""");
        json.Should().Contain(@"""OAuthHeaders"":{""AccessToken"":""AccessToken"",""TokenType"":""TokenType"",""ExpiresIn"":""12345"",""RefreshToken"":""RefreshToken"",""ExpiresInRefreshToken"":""54321""}");
        json.Should().Contain(@$"""AuthenticatedAt"":""{DateTimeOffset.UtcNow.Date:yyyy-MM-dd}");
    }

    [Fact]
    public void DeserialiazeWithEmptyPropertiesShouldDeserialize()
    {
        //Arrange
        var json = @"{}";

        //Act
        var headers = AuthorizationHeadersJsonSerializer.Deserialize(json);

        //Assert
        headers.Should().NotBeNull();
        headers.Should().BeEmpty();
        headers.OAuthHeaders.Should().BeNull();
        headers.ExpiresIn.Should().BeNull();
        headers.AuthenticatedAt.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public void DeserialiazeWithPropertiesEmptyShouldDeserialize()
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
        headers.Should().NotBeNull();
        headers.Should().BeEmpty();
        headers.OAuthHeaders.Should().BeNull();
        headers.ExpiresIn.Should().BeNull();
        headers.AuthenticatedAt.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public void DeserialiazeWithPropertiesEmptyOAuthShouldDeserialize()
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
        headers.Should().NotBeNull();
        headers.Should().BeEmpty();
        headers.OAuthHeaders.Should().BeNull();
        headers.ExpiresIn.Should().BeNull();
        headers.AuthenticatedAt.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public void DeserialiazeWithPropertiesAndEmptyOAuthShouldDeserialize()
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
        headers.Should().NotBeNull();
        headers.Should().NotBeEmpty();
        headers.Should().Contain(a => a.Key == "Header1" && a.Value == "Value1");
        headers.OAuthHeaders.Should().BeNull();
        headers.ExpiresIn.Should().NotBeNull();
        headers.ExpiresIn.Value.Should().Be(TimeSpan.FromMinutes(3));
        headers.AuthenticatedAt.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public void DeserialiazeWithPropertiesShouldDeserialize()
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
        headers.Should().NotBeNull();
        headers.Should().NotBeEmpty();
        headers.Should().Contain(a => a.Key == "Header1" && a.Value == "Value1");
        headers.OAuthHeaders.Should().NotBeNull();
        headers.OAuthHeaders.AccessToken.Should().Be("AccessToken");
        headers.OAuthHeaders.TokenType.Should().Be("Basic");
        headers.OAuthHeaders.ExpiresIn.Should().NotBeNull();
        headers.OAuthHeaders.ExpiresIn.Value.Should().Be(120);
        headers.OAuthHeaders.RefreshToken.Should().BeNull();
        headers.OAuthHeaders.ExpiresInRefreshToken.Should().BeNull();
        headers.ExpiresIn.Should().NotBeNull();
        headers.ExpiresIn.Value.Should().Be(TimeSpan.FromMinutes(3));
        JsonSerializer.Serialize(headers.AuthenticatedAt).Should().Be(@"""2024-04-05T19:47:35.1564179+00:00""");
    }

    [Fact]
    public void SerialiazeWithOAuthPropertiesWithoutExpirationsShouldSerialize()
    {
        //Arrange
        AuthorizationHeaders headers = new OAuthHeaders("AccessToken", "TokenType");

        //Act
        var json = AuthorizationHeadersJsonSerializer.Serialize(headers);

        //Assert
        json.Should().Contain(@"""OAuthHeaders"":{""AccessToken"":""AccessToken"",""TokenType"":""TokenType"",""ExpiresIn"":null,""RefreshToken"":null,""ExpiresInRefreshToken"":null}");
    }

    [Fact]
    public void DeserialiazeWithUnexpectedTokenShouldThrow()
    {
        //Arrange
        var json = @"""not-an-object""";

        //Act
        Action act = () => AuthorizationHeadersJsonSerializer.Deserialize(json);

        //Assert
        act.Should().Throw<JsonException>().WithMessage("*Expected EndObject token.*");
    }

    [Fact]
    public void DeserialiazeWithOAuthPropertiesWithoutExpirationsShouldDeserialize()
    {
        //Arrange
        var json = @"{
                         ""Headers"":{""Authorization"":""Bearer AccessToken""},
                         ""OAuthHeaders"":{
                             ""AccessToken"":""AccessToken"",
                             ""TokenType"":""Bearer"",
                             ""ExpiresIn"":"""",
                             ""RefreshToken"":""RefreshToken"",
                             ""ExpiresInRefreshToken"":""500""
                         }
                      }";

        //Act
        var headers = AuthorizationHeadersJsonSerializer.Deserialize(json);

        //Assert
        headers.Should().NotBeNull();
        headers.OAuthHeaders.Should().NotBeNull();
        headers.OAuthHeaders!.ExpiresIn.Should().BeNull();
        headers.OAuthHeaders.RefreshToken.Should().Be("RefreshToken");
        headers.OAuthHeaders.ExpiresInRefreshToken.Should().Be(500);
    }

    [Fact]
    public void DeserialiazeWithEmptyHeaderKeyOrValueShouldIgnoreThem()
    {
        //Arrange
        var json = @"{
                         ""Headers"":{
                             """":""IgnoredKey"",
                             ""IgnoredValue"":""""
                         },
                         ""ExpiresIn"":""00:03:00""
                      }";

        //Act
        var headers = AuthorizationHeadersJsonSerializer.Deserialize(json);

        //Assert
        headers.Should().NotBeNull();
        headers.Should().BeEmpty();
        headers.ExpiresIn.Should().Be(TimeSpan.FromMinutes(3));
    }
}
