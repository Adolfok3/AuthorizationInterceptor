using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AuthorizationInterceptor.Tests.Handlers;

public class OAuth2ClientCredentialsAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_WithClientSecretBasic_ShouldRequestAndReturnToken()
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"access-token","token_type":"Bearer","expires_in":3600}""", Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport, options =>
        {
            options.ClientId = "my client";
            options.ClientSecret = "s:ecret";
            options.Scopes.Add("orders.read");
            options.Scopes.Add("orders.write");
            options.AdditionalParameters.Add("audience", "orders-api");
        });

        var headers = await handler.AuthenticateAsync(null, CancellationToken.None);

        Assert.NotNull(headers);
        Assert.Equal("Bearer access-token", headers["Authorization"]);
        Assert.Equal(3600, headers.OAuthHeaders?.ExpiresIn);
        Assert.Equal(HttpMethod.Post, transport.Method);
        Assert.Equal(new Uri("https://identity.example.com/token"), transport.RequestUri);
        Assert.Equal("Basic", transport.AuthorizationScheme);
        Assert.Equal("application/x-www-form-urlencoded", transport.ContentType);
        Assert.Equal("my+client:s%3Aecret", DecodeBasicCredentials(transport.AuthorizationParameter!));
        var form = ParseForm(transport.Body);
        Assert.Equal("client_credentials", form["grant_type"]);
        Assert.Equal("orders.read orders.write", form["scope"]);
        Assert.Equal("orders-api", form["audience"]);
        Assert.Equal(3, form.Count);
    }

    [Fact]
    public async Task AuthenticateAsync_WithClientSecretPost_ShouldSendCredentialsInBody()
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"access-token","token_type":"Bearer"}""", Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport, options =>
            options.ClientAuthenticationMethod = OAuth2ClientAuthenticationMethod.ClientSecretPost);

        var headers = await handler.AuthenticateAsync(null, CancellationToken.None);

        Assert.NotNull(headers);
        Assert.Equal("Bearer access-token", headers["Authorization"]);
        Assert.Null(headers.ExpiresIn);
        Assert.Null(headers.OAuthHeaders?.ExpiresIn);
        Assert.Null(transport.AuthorizationScheme);
        Assert.Equal("application/x-www-form-urlencoded", transport.ContentType);
        var form = ParseForm(transport.Body);
        Assert.Equal("client_credentials", form["grant_type"]);
        Assert.Equal("client-id", form["client_id"]);
        Assert.Equal("client-secret", form["client_secret"]);
        Assert.Equal(3, form.Count);
    }

    [Theory]
    [InlineData(OAuth2ClientAuthenticationMethod.ClientSecretBasic)]
    [InlineData(OAuth2ClientAuthenticationMethod.ClientSecretPost)]
    public async Task AuthenticateAsync_WithSpecialCharacters_ShouldEncodeCredentialsAndAdditionalParameters(
        OAuth2ClientAuthenticationMethod authenticationMethod)
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"token","token_type":"Bearer"}""", Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport, options =>
        {
            options.ClientAuthenticationMethod = authenticationMethod;
            options.ClientId = "client +:&é";
            options.ClientSecret = "secret +:&雪";
            options.AdditionalParameters.Add("resource", "https://api.example.com/items?name=a+b&lang=pt BR");
        });

        await handler.AuthenticateAsync(null, CancellationToken.None);

        Assert.Equal("application/x-www-form-urlencoded", transport.ContentType);
        var form = ParseForm(transport.Body);
        Assert.Equal("client_credentials", form["grant_type"]);
        Assert.Equal("https://api.example.com/items?name=a+b&lang=pt BR", form["resource"]);
        Assert.False(form.ContainsKey("scope"));

        if (authenticationMethod == OAuth2ClientAuthenticationMethod.ClientSecretBasic)
        {
            Assert.Equal("Basic", transport.AuthorizationScheme);
            Assert.Equal("client+%2B%3A%26%C3%A9:secret+%2B%3A%26%E9%9B%AA", DecodeBasicCredentials(transport.AuthorizationParameter!));
            Assert.Equal(2, form.Count);
        }
        else
        {
            Assert.Null(transport.AuthorizationScheme);
            Assert.Equal("client +:&é", form["client_id"]);
            Assert.Equal("secret +:&雪", form["client_secret"]);
            Assert.Equal(4, form.Count);
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task AuthenticateAsync_WhenEndpointRejectsRequest_ShouldThrowHttpRequestException(HttpStatusCode statusCode)
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("""{"error":"invalid_client"}""", Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);

        var action = async () => await handler.AuthenticateAsync(null, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(action);
        Assert.Equal(statusCode, exception.StatusCode);
    }

    [Theory]
    [InlineData("null", "access_token")]
    [InlineData("{\"token_type\":\"Bearer\"}", "access_token")]
    [InlineData("{\"access_token\":null,\"token_type\":\"Bearer\"}", "access_token")]
    [InlineData("{\"access_token\":\"\",\"token_type\":\"Bearer\"}", "access_token")]
    [InlineData("{\"access_token\":\" \\t\\r\\n\",\"token_type\":\"Bearer\"}", "access_token")]
    [InlineData("{\"access_token\":\"token\"}", "token_type")]
    [InlineData("{\"access_token\":\"token\",\"token_type\":null}", "token_type")]
    [InlineData("{\"access_token\":\"token\",\"token_type\":\"\"}", "token_type")]
    [InlineData("{\"access_token\":\"token\",\"token_type\":\" \\t\\r\\n\"}", "token_type")]
    public async Task AuthenticateAsync_WhenRequiredResponsePropertyIsMissingOrInvalid_ShouldThrow(string json, string property)
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);

        var action = async () => await handler.AuthenticateAsync(null, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Contains(property, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{\"access_token\":")]
    public async Task AuthenticateAsync_WhenResponseIsInvalidJson_ShouldThrowJsonException(string json)
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);

        var action = async () => await handler.AuthenticateAsync(null, CancellationToken.None);

        await Assert.ThrowsAsync<JsonException>(action);
    }

    [Theory]
    [InlineData("null", null)]
    [InlineData("120.5", 120.5)]
    public async Task AuthenticateAsync_ShouldPreserveTokenTypeAndExpiration(string expirationJson, double? expiresIn)
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"access_token":"token","token_type":"Custom","expires_in":{{expirationJson}},"extra":"ignored"}""",
                Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);

        var headers = await handler.AuthenticateAsync(null, CancellationToken.None);

        Assert.NotNull(headers);
        Assert.Equal("Custom token", headers["Authorization"]);
        Assert.NotNull(headers.OAuthHeaders);
        Assert.Equal(expiresIn, headers.OAuthHeaders.ExpiresIn);
        Assert.Equal(expiresIn.HasValue ? TimeSpan.FromSeconds(expiresIn.Value) : (TimeSpan?)null, headers.ExpiresIn);
        Assert.Single(headers);
    }

    [Fact]
    public async Task AuthenticateAsync_WithExpiredHeaders_ShouldAcquireNewClientCredentialsToken()
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"new-token","token_type":"Bearer","expires_in":300}""", Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);
        AuthorizationHeaders expiredHeaders = new OAuthHeaders("old-token", "Bearer", 1, "old-refresh-token", 600);

        var headers = await handler.AuthenticateAsync(expiredHeaders, CancellationToken.None);

        Assert.NotNull(headers);
        Assert.Equal("Bearer new-token", headers["Authorization"]);
        Assert.Equal("grant_type=client_credentials", transport.Body);
        Assert.Equal("Bearer old-token", expiredHeaders["Authorization"]);
        Assert.Null(headers.OAuthHeaders?.RefreshToken);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenTransportFails_ShouldPropagateException()
    {
        var failure = new HttpRequestException("Token endpoint unavailable.");
        var handler = CreateHandler(new RecordingHandler(_ => throw failure));

        var action = async () => await handler.AuthenticateAsync(null, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(action);
        Assert.Same(failure, exception);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCanceledDuringRequest_ShouldCancelTransport()
    {
        using var cancellation = new CancellationTokenSource();
        var requestStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new RecordingHandler(async (_, cancellationToken) =>
        {
            requestStarted.SetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var handler = CreateHandler(transport);

        var authentication = handler.AuthenticateAsync(null, cancellation.Token).AsTask();
        try
        {
            var transportCancellation = await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => authentication.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.True(transportCancellation.IsCancellationRequested);
        }
        finally
        {
            cancellation.Cancel();
        }
    }

    private static OAuth2ClientCredentialsAuthenticationHandler CreateHandler(
        RecordingHandler transport,
        Action<OAuth2ClientCredentialsOptions>? configure = null)
    {
        var client = new HttpClient(transport);
        var factory = Substitute.For<IHttpClientFactory>();
        const string httpClientName = "TargetApiOAuth2ClientCredentialsAuthenticationHandler";
        factory.CreateClient(httpClientName).Returns(client);

        var options = new OAuth2ClientCredentialsOptions
        {
            TokenEndpoint = new Uri("https://identity.example.com/token"),
            ClientId = "client-id",
            ClientSecret = "client-secret"
        };
        configure?.Invoke(options);

        return new OAuth2ClientCredentialsAuthenticationHandler(factory, options, httpClientName);
    }

    private static string DecodeBasicCredentials(string credentials)
        => Encoding.UTF8.GetString(Convert.FromBase64String(credentials));

    private static Dictionary<string, string> ParseForm(string body)
        => body.Split('&')
            .Select(parameter => parameter.Split('=', 2))
            .ToDictionary(parameter => WebUtility.UrlDecode(parameter[0]), parameter => WebUtility.UrlDecode(parameter[1]));

    private sealed class RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            : this((request, _) => Task.FromResult(responseFactory(request)))
        {
        }

        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string? ContentType { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return await responseFactory(request, cancellationToken);
        }
    }
}
