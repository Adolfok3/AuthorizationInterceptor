using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Options;
using System.Net;
using System.Text;

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
        Assert.Equal("my+client:s%3Aecret", DecodeBasicCredentials(transport.AuthorizationParameter!));
        Assert.Contains("grant_type=client_credentials", transport.Body);
        Assert.Contains("scope=orders.read+orders.write", transport.Body);
        Assert.Contains("audience=orders-api", transport.Body);
        Assert.DoesNotContain("client_secret", transport.Body);
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
        Assert.Null(transport.AuthorizationScheme);
        Assert.Contains("client_id=client-id", transport.Body);
        Assert.Contains("client_secret=client-secret", transport.Body);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenEndpointRejectsCredentials_ShouldThrowHttpRequestException()
    {
        var transport = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"error":"invalid_client"}""", Encoding.UTF8, "application/json")
        });
        var handler = CreateHandler(transport);

        var action = async () => await handler.AuthenticateAsync(null, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(action);
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Theory]
    [InlineData("{\"token_type\":\"Bearer\"}", "access_token")]
    [InlineData("{\"access_token\":\"token\"}", "token_type")]
    public async Task AuthenticateAsync_WhenRequiredResponsePropertyIsMissing_ShouldThrow(string json, string property)
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

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return responseFactory(request);
        }
    }
}
