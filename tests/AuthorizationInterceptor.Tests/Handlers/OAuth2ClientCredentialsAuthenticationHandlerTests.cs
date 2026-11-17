using System.Net;
using System.Text;
using System.Text.Json;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Options;
using FluentAssertions;

namespace AuthorizationInterceptor.Tests.Handlers;

public class OAuth2ClientCredentialsAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsyncWithClientSecretBasicShouldRequestAndReturnToken()
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

        headers.Should().NotBeNull();
        headers["Authorization"].Should().Be("Bearer access-token");
        (headers.OAuthHeaders?.ExpiresIn).Should().Be(3600);
        transport.Method.Should().Be(HttpMethod.Post);
        transport.RequestUri.Should().Be(new Uri("https://identity.example.com/token"));
        transport.AuthorizationScheme.Should().Be("Basic");
        transport.ContentType.Should().Be("application/x-www-form-urlencoded");
        DecodeBasicCredentials(transport.AuthorizationParameter!).Should().Be("my+client:s%3Aecret");
        var form = ParseForm(transport.Body);
        form["grant_type"].Should().Be("client_credentials");
        form["scope"].Should().Be("orders.read orders.write");
        form["audience"].Should().Be("orders-api");
        form.Count.Should().Be(3);
    }

    [Fact]
    public async Task AuthenticateAsyncWithClientSecretPostShouldSendCredentialsInBody()
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"access-token","token_type":"Bearer"}""", Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport, options =>
            options.ClientAuthenticationMethod = OAuth2ClientAuthenticationMethod.ClientSecretPost);

        var headers = await handler.AuthenticateAsync(null, CancellationToken.None);

        headers.Should().NotBeNull();
        headers["Authorization"].Should().Be("Bearer access-token");
        headers.ExpiresIn.Should().BeNull();
        (headers.OAuthHeaders?.ExpiresIn).Should().BeNull();
        transport.AuthorizationScheme.Should().BeNull();
        transport.ContentType.Should().Be("application/x-www-form-urlencoded");
        var form = ParseForm(transport.Body);
        form["grant_type"].Should().Be("client_credentials");
        form["client_id"].Should().Be("client-id");
        form["client_secret"].Should().Be("client-secret");
        form.Count.Should().Be(3);
    }

    [Theory]
    [InlineData(OAuth2ClientAuthenticationMethod.ClientSecretBasic)]
    [InlineData(OAuth2ClientAuthenticationMethod.ClientSecretPost)]
    public async Task AuthenticateAsyncWithSpecialCharactersShouldEncodeCredentialsAndAdditionalParameters(
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

        transport.ContentType.Should().Be("application/x-www-form-urlencoded");
        var form = ParseForm(transport.Body);
        form["grant_type"].Should().Be("client_credentials");
        form["resource"].Should().Be("https://api.example.com/items?name=a+b&lang=pt BR");
        form.ContainsKey("scope").Should().BeFalse();

        if (authenticationMethod == OAuth2ClientAuthenticationMethod.ClientSecretBasic)
        {
            transport.AuthorizationScheme.Should().Be("Basic");
            DecodeBasicCredentials(transport.AuthorizationParameter!).Should().Be("client+%2B%3A%26%C3%A9:secret+%2B%3A%26%E9%9B%AA");
            form.Count.Should().Be(2);
        }
        else
        {
            transport.AuthorizationScheme.Should().BeNull();
            form["client_id"].Should().Be("client +:&é");
            form["client_secret"].Should().Be("secret +:&雪");
            form.Count.Should().Be(4);
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task AuthenticateAsyncWhenEndpointRejectsRequestShouldThrowHttpRequestException(HttpStatusCode statusCode)
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("""{"error":"invalid_client"}""", Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);

        var action = async () => await handler.AuthenticateAsync(null, CancellationToken.None);

        var exception = await action.Should().ThrowExactlyAsync<HttpRequestException>();
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
    public async Task AuthenticateAsyncWhenRequiredResponsePropertyIsMissingOrInvalidShouldThrow(string json, string property)
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);

        var action = async () => await handler.AuthenticateAsync(null, CancellationToken.None);

        var exception = await action.Should().ThrowExactlyAsync<InvalidOperationException>();
        exception.Message.Should().Contain(property);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{\"access_token\":")]
    public async Task AuthenticateAsyncWhenResponseIsInvalidJsonShouldThrowJsonException(string json)
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);

        var action = async () => await handler.AuthenticateAsync(null, CancellationToken.None);

        await action.Should().ThrowExactlyAsync<JsonException>();
    }

    [Theory]
    [InlineData("null", null)]
    [InlineData("120.5", 120.5)]
    public async Task AuthenticateAsyncShouldPreserveTokenTypeAndExpiration(string expirationJson, double? expiresIn)
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"access_token":"token","token_type":"Custom","expires_in":{{expirationJson}},"extra":"ignored"}""",
                Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);

        var headers = await handler.AuthenticateAsync(null, CancellationToken.None);

        headers.Should().NotBeNull();
        headers["Authorization"].Should().Be("Custom token");
        headers.OAuthHeaders.Should().NotBeNull();
        headers.OAuthHeaders.ExpiresIn.Should().Be(expiresIn);
        headers.ExpiresIn.Should().Be(expiresIn.HasValue ? TimeSpan.FromSeconds(expiresIn.Value) : (TimeSpan?)null);
        headers.Should().ContainSingle();
    }

    [Fact]
    public async Task AuthenticateAsyncWithExpiredHeadersShouldAcquireNewClientCredentialsToken()
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"access_token":"new-token","token_type":"Bearer","expires_in":300}""", Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);
        AuthorizationHeaders expiredHeaders = new OAuthHeaders("old-token", "Bearer", 1, "old-refresh-token", 600);

        var headers = await handler.AuthenticateAsync(expiredHeaders, CancellationToken.None);

        headers.Should().NotBeNull();
        headers["Authorization"].Should().Be("Bearer new-token");
        transport.Body.Should().Be("grant_type=client_credentials");
        expiredHeaders["Authorization"].Should().Be("Bearer old-token");
        (headers.OAuthHeaders?.RefreshToken).Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsyncWhenTransportFailsShouldPropagateException()
    {
        var failure = new HttpRequestException("Token endpoint unavailable.");
        var handler = CreateHandler(new RecordingHandler(_ => throw failure));

        var action = async () => await handler.AuthenticateAsync(null, CancellationToken.None);

        var exception = await action.Should().ThrowExactlyAsync<HttpRequestException>();
        exception.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task AuthenticateAsyncWhenCanceledDuringRequestShouldCancelTransport()
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

            await (() => authentication.WaitAsync(TimeSpan.FromSeconds(5))).Should().ThrowAsync<OperationCanceledException>(
);
            transportCancellation.IsCancellationRequested.Should().BeTrue();
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
