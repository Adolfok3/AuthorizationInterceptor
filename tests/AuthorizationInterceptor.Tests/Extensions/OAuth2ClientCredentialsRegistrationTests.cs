using AuthorizationInterceptor.Extensions;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

namespace AuthorizationInterceptor.Tests.Extensions;

public class OAuth2ClientCredentialsRegistrationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RegisteredHandler_ShouldUseAuthenticationClientConfiguredBeforeOrAfterRegistration(bool configureAuthenticationClientFirst)
    {
        var tokenEndpoint = new TokenEndpointHandler();
        var targetApi = new TargetApiHandler();
        var services = new ServiceCollection();

        void ConfigureAuthenticationClient() => services
            .AddHttpClient("OrdersApiOAuth2ClientCredentialsAuthenticationHandler", client =>
                client.DefaultRequestHeaders.Add("X-Token-Client", "configured"))
            .ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);

        if (configureAuthenticationClientFirst)
            ConfigureAuthenticationClient();

        services.AddHttpClient("OrdersApi")
            .AddClientCredentialsAuthorizationInterceptorHandler(options =>
            {
                options.TokenEndpoint = new Uri("https://identity.example.com/token");
                options.ClientId = "orders-client";
                options.ClientSecret = "orders-secret";
            })
            .ConfigurePrimaryHttpMessageHandler(() => targetApi);

        if (!configureAuthenticationClientFirst)
            ConfigureAuthenticationClient();

        await using var provider = services.BuildServiceProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("OrdersApi");

        using var response = await client.GetAsync("https://orders.example.com/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, tokenEndpoint.RequestCount);
        Assert.Equal("Bearer", targetApi.AuthorizationScheme);
        Assert.Equal("client-credentials-token", targetApi.AuthorizationParameter);
    }

    [Fact]
    public async Task RegisteredHandlers_ForDifferentNamedClients_ShouldKeepCredentialsAndCachedTokensSeparate()
    {
        var services = new ServiceCollection();
        var cache = new InterceptorCache();
        var tokenEndpoints = new List<CallbackHttpMessageHandler>();
        var receivedTokens = new List<string?>();
        services.AddSingleton(cache);

        foreach (var apiName in new[] { "OrdersApi", "BillingApi" })
        {
            var tokenEndpoint = new CallbackHttpMessageHandler((request, _) =>
            {
                Assert.Equal(new Uri($"https://identity.example.com/{apiName}/token"), request.RequestUri);
                Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
                Assert.Equal(
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiName}-client:{apiName}-secret")),
                    request.Headers.Authorization?.Parameter);
                return CreateTokenResponse($"{apiName}-token");
            });
            tokenEndpoints.Add(tokenEndpoint);

            services.AddHttpClient($"{apiName}OAuth2ClientCredentialsAuthenticationHandler")
                .ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);
            services.AddHttpClient(apiName)
                .AddClientCredentialsAuthorizationInterceptorHandler(authentication =>
                {
                    authentication.TokenEndpoint = new Uri($"https://identity.example.com/{apiName}/token");
                    authentication.ClientId = $"{apiName}-client";
                    authentication.ClientSecret = $"{apiName}-secret";
                }, interceptor => interceptor.UseCustomInterceptor<CachingInterceptor>())
                .ConfigurePrimaryHttpMessageHandler(() => new CallbackHttpMessageHandler((request, _) =>
                {
                    receivedTokens.Add(request.Headers.Authorization?.Parameter);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }));
        }

        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using var ordersClient = factory.CreateClient("OrdersApi");
        using var billingClient = factory.CreateClient("BillingApi");

        using var firstOrdersResponse = await ordersClient.GetAsync("https://orders.example.com/orders");
        using var firstBillingResponse = await billingClient.GetAsync("https://billing.example.com/invoices");
        using var secondOrdersResponse = await ordersClient.GetAsync("https://orders.example.com/orders");
        using var secondBillingResponse = await billingClient.GetAsync("https://billing.example.com/invoices");

        Assert.Equal(new[] { "OrdersApi-token", "BillingApi-token", "OrdersApi-token", "BillingApi-token" }, receivedTokens);
        Assert.All(tokenEndpoints, endpoint => Assert.Equal(1, endpoint.RequestCount));
        Assert.Equal(new[] { "BillingApi", "OrdersApi" }, cache.Headers.Keys.Order());
    }

    [Fact]
    public async Task RegisteredHandler_WithCustomUnauthenticatedPredicate_ShouldRefreshTokenAndRetryRequest()
    {
        var tokenEndpoint = new CallbackHttpMessageHandler((_, count) => CreateTokenResponse($"token-{count}"));
        var receivedTokens = new List<string?>();
        var targetApi = new CallbackHttpMessageHandler((request, count) =>
        {
            receivedTokens.Add(request.Headers.Authorization?.Parameter);
            return new HttpResponseMessage(count == 1 ? HttpStatusCode.Forbidden : HttpStatusCode.OK);
        });
        var services = new ServiceCollection();
        services.AddHttpClient("OrdersApiOAuth2ClientCredentialsAuthenticationHandler")
            .ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);
        services.AddHttpClient("OrdersApi")
            .AddClientCredentialsAuthorizationInterceptorHandler(authentication =>
            {
                authentication.TokenEndpoint = new Uri("https://identity.example.com/token");
                authentication.ClientId = "orders-client";
                authentication.ClientSecret = "orders-secret";
            }, interceptor => interceptor.UnauthenticatedPredicate = response => response.StatusCode == HttpStatusCode.Forbidden)
            .ConfigurePrimaryHttpMessageHandler(() => targetApi);

        await using var provider = services.BuildServiceProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("OrdersApi");

        using var response = await client.GetAsync("https://orders.example.com/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { "token-1", "token-2" }, receivedTokens);
        Assert.Equal(2, tokenEndpoint.RequestCount);
        Assert.Equal(2, targetApi.RequestCount);
    }

    [Fact]
    public async Task RegisteredHandler_WithInterceptorDependenciesAndCacheKeyBuilder_ShouldCacheTokensPerTenant()
    {
        var cache = new InterceptorCache();
        var tokenEndpoint = new CallbackHttpMessageHandler((_, count) => CreateTokenResponse($"token-{count}"));
        var receivedTokens = new List<string?>();
        var targetApi = new CallbackHttpMessageHandler((request, _) =>
        {
            receivedTokens.Add(request.Headers.Authorization?.Parameter);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var services = new ServiceCollection();
        services.AddHttpClient("OrdersApiOAuth2ClientCredentialsAuthenticationHandler")
            .ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);
        services.AddHttpClient("OrdersApi")
            .AddClientCredentialsAuthorizationInterceptorHandler(authentication =>
            {
                authentication.TokenEndpoint = new Uri("https://identity.example.com/token");
                authentication.ClientId = "orders-client";
                authentication.ClientSecret = "orders-secret";
            }, interceptor =>
            {
                interceptor.UseCustomInterceptor<CachingInterceptor>(dependencies => dependencies.AddSingleton(cache));
                interceptor.CacheKeyBuilder = accessor => accessor.HttpContext?.Request.Headers["X-Tenant"].ToString();
            })
            .ConfigurePrimaryHttpMessageHandler(() => targetApi);

        await using var provider = services.BuildServiceProvider();
        services.MakeReadOnly();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("OrdersApi");
        var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Request.Headers["X-Tenant"] = "tenant-a";

        using var firstResponse = await client.GetAsync("https://orders.example.com/orders");
        using var cachedResponse = await client.GetAsync("https://orders.example.com/orders");
        httpContextAccessor.HttpContext.Request.Headers["X-Tenant"] = "tenant-b";
        using var otherTenantResponse = await client.GetAsync("https://orders.example.com/orders");

        Assert.Equal(new[] { "token-1", "token-1", "token-2" }, receivedTokens);
        Assert.Equal(2, tokenEndpoint.RequestCount);
        Assert.Equal(new[] { "OrdersApi_tenant-a", "OrdersApi_tenant-b" }, cache.Headers.Keys.Order());
    }

    private static HttpResponseMessage CreateTokenResponse(string accessToken) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            $$"""{"access_token":"{{accessToken}}","token_type":"Bearer","expires_in":300}""",
            Encoding.UTF8,
            "application/json")
    };

    private sealed class CallbackHttpMessageHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request, ++RequestCount));
    }

    private sealed class InterceptorCache
    {
        public Dictionary<string, AuthorizationHeaders?> Headers { get; } = [];
    }

    private sealed class CachingInterceptor(InterceptorCache cache) : IAuthorizationInterceptor
    {
        public ValueTask<AuthorizationHeaders?> GetHeadersAsync(string key, CancellationToken cancellationToken)
            => ValueTask.FromResult(cache.Headers.GetValueOrDefault(key));

        public ValueTask UpdateHeadersAsync(string key, AuthorizationHeaders? expiredHeaders, AuthorizationHeaders? newHeaders, CancellationToken cancellationToken)
        {
            cache.Headers[key] = newHeaders;
            return ValueTask.CompletedTask;
        }
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
            Assert.Equal("configured", Assert.Single(request.Headers.GetValues("X-Token-Client")));
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
            Assert.False(request.Headers.Contains("X-Token-Client"));
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
