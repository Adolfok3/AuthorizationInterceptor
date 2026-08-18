using AuthorizationInterceptor.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

namespace AuthorizationInterceptor.Tests.Extensions;

public class OAuth2ClientCredentialsRegistrationTests
{
    [Fact]
    public async Task RegisteredHandler_ShouldAcquireTokenAndAuthorizeTargetRequest()
    {
        var tokenEndpoint = new TokenEndpointHandler();
        var targetApi = new TargetApiHandler();
        var services = new ServiceCollection();

        services.AddHttpClient("OrdersApiOAuth2ClientCredentialsAuthenticationHandler")
            .ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);

        services.AddHttpClient("OrdersApi")
            .AddClientCredentialsAuthorizationInterceptorHandler(options =>
            {
                options.TokenEndpoint = new Uri("https://identity.example.com/token");
                options.ClientId = "orders-client";
                options.ClientSecret = "orders-secret";
            })
            .ConfigurePrimaryHttpMessageHandler(() => targetApi);

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("OrdersApi");

        var response = await client.GetAsync("https://orders.example.com/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, tokenEndpoint.RequestCount);
        Assert.Equal("Bearer", targetApi.AuthorizationScheme);
        Assert.Equal("client-credentials-token", targetApi.AuthorizationParameter);
    }

    private sealed class TokenEndpointHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(new Uri("https://identity.example.com/token"), request.RequestUri);
            Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
            Assert.Contains("grant_type=client_credentials", await request.Content!.ReadAsStringAsync(cancellationToken));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"client-credentials-token","token_type":"Bearer","expires_in":300}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed class TargetApiHandler : HttpMessageHandler
    {
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
