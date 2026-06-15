![AuthorizationInterceptor Icon](./resources/icon.png)

# Authorization Interceptor

A lightweight .NET library that automatically manages HTTP authentication headers for `HttpClient`. When a request receives a 401 response, the interceptor re-authenticates and retries with fresh headers — no manual token management required.

[![GitHub Actions](https://github.com/Adolfok3/authorizationinterceptor/actions/workflows/main.yml/badge.svg)](https://github.com/Adolfok3/AuthorizationInterceptor/actions)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](./LICENSE)
[![Codecov](https://codecov.io/github/Adolfok3/AuthorizationInterceptor/graph/badge.svg?token=PHBV20RCQK)](https://codecov.io/github/Adolfok3/AuthorizationInterceptor)
[![NuGet Version](https://img.shields.io/nuget/vpre/AuthorizationInterceptor)](https://www.nuget.org/packages/AuthorizationInterceptor)
[![.NET Support](https://img.shields.io/badge/.NET-8%2C9%2C10-blue)](https://dotnet.microsoft.com/download)

## Features

- **Automatic retry on auth failure** — intercepts 401 responses and retries with fresh headers
- **OAuth2 refresh token support** — reuse existing tokens via `RefreshToken` flow
- **Custom header support** — return any key-value authorization headers
- **Multiple cache backends** — in-memory, distributed (Redis/NCache), or hybrid caching
- **Distributed concurrency-safe** — safe for multi-instance/Kubernetes deployments
- **Extensible interceptor chain** — compose your own caching and logging strategies
- **Multi-target framework support** — .NET 8+

## Quick Start

Install the core package:

```
dotnet add package AuthorizationInterceptor
```

### Step 1: Implement authentication logic

Create a class that implements `IAuthenticationHandler`:

```csharp
public class TargetApiAuth : IAuthenticationHandler
{
    private readonly HttpClient _client;

    public TargetApiAuth(HttpClient client)
    {
        _client = client;
    }

    public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(
        AuthorizationHeaders? expiredHeaders, CancellationToken ct)
    {
        if (expiredHeaders == null)
        {
            // First login — request a fresh token
            var response = await _client.PostAsync("auth", content: null, ct);
        }
        else
        {
            // Token expired — refresh it using the existing refresh token
            // This step is only applicable to APIs integrating with OAuth refresh tokens
            var refreshToken = expiredHeaders.OAuthHeaders!.RefreshToken;
            var response = await _client.PostAsync($"refresh?refresh={refreshToken}", content: null, ct);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var tokens = JsonSerializer.Deserialize<UserTokens>(json)!;

        return new OAuthHeaders(
            accessToken: tokens.AccessToken,
            tokenType: tokens.TokenType,
            expiresIn: tokens.ExpiresIn,
            refreshToken: tokens.RefreshToken,
            refreshTokenExpiresIn: tokens.RefreshAccessTokenExpiresIn);
    }
}

public record UserTokens(string AccessToken, string TokenType, int ExpiresIn, string RefreshToken, int RefreshAccessTokenExpiresIn);
```

### Step 2: Register the handler

```csharp
builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuth>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://targetapi.com"));
```

That's it. Calls to this HttpClient will automatically retry with fresh authorization headers when a 401 is received.

## Caching & Interceptors

By default, without any cache interceptor, a new access token is generated on every expiration. For production deployments, use one of the cache interceptors below to avoid redundant authentication calls.

### Available Packages

| Package                                                                                                                                     | Use case                                                                   |
| ------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| [AuthorizationInterceptor.Extensions.MemoryCache](https://www.nuget.org/packages/AuthorizationInterceptor.Extensions.MemoryCache)           | Local in-memory caching — good for single-instance apps                    |
| [AuthorizationInterceptor.Extensions.DistributedCache](https://www.nuget.org/packages/AuthorizationInterceptor.Extensions.DistributedCache) | Distributed caching (Redis, NCache, etc.) — for multi-instance deployments |
| [AuthorizationInterceptor.Extensions.HybridCache](https://www.nuget.org/packages/AuthorizationInterceptor.Extensions.HybridCache)           | Memory + distributed cache combined — recommended for production           |

### Recommended configuration: Hybrid Cache

```csharp
builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
    {
        options.UseHybridCacheInterceptor(); // memory → distributed → auth handler
    })
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://targetapi.com"));
```

This uses an in-memory cache first (fastest), falls back to distributed cache, then calls the authentication handler only when no cached token exists. This ensures all instances share the same token and avoids redundant login calls.

## Options & Customization

### Retry on 403 as well as 401

Some APIs return 403 instead of 401 when tokens expire:

```csharp
builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
    {
        options.UnauthenticatedPredicate = response =>
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized;
    });
```

### Multiple HttpClient instances with different auth data

When you need to pass extra dependencies into your authentication handler:

```csharp
builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler((sp) =>
        ActivatorUtilities.CreateInstance<TargetApiAuth>(sp, someOtherDependency));
```

### Per-request cache keys

When the same `HttpClient` is used for the same target API, but authorization headers must be cached separately by a request value, configure `CacheKeyBuilder`.

This is useful when a single integration can authenticate on behalf of different users, tenants, stores, organizations, or any other request-scoped identifier. The value returned by `CacheKeyBuilder` is appended to the cache key used by the configured cache interceptor.

Example using the authenticated user:

```csharp
builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
    {
        options.UseHybridCacheInterceptor();
        options.CacheKeyBuilder = accessor =>
            accessor.HttpContext?.User.FindFirst("sub")?.Value;
    });
```

Example using a route or query value:

```csharp
builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
    {
        options.UseDistributedCacheInterceptor();
        options.CacheKeyBuilder = accessor =>
        {
            var httpContext = accessor.HttpContext;
            var storeId = httpContext?.Request.RouteValues["storeId"]?.ToString()
                ?? httpContext?.Request.Query["storeId"].ToString();

            return string.IsNullOrWhiteSpace(storeId) ? null : storeId;
        };
    });
```

With this configuration, requests using the same `HttpClient` but different `HttpContext.Request` values will not share the same cached authorization headers.

If `CacheKeyBuilder` returns `null` or an empty value, the interceptor uses the default cache key for the `HttpClient` name.

### Custom interceptors

Add custom logic steps to the interceptor chain:

```csharp
builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
    {
        options.UseMemoryCacheInterceptor();
        options.UseCustomInterceptor<MyLoggingInterceptor>();
    });
```

Implement `IAuthorizationInterceptor`:

```csharp
public class MyLoggingInterceptor : IAuthorizationInterceptor
{
    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(
        string name, CancellationToken ct, string? cacheKeySuffix = null)
        => new(new AuthorizationHeaders());

    public ValueTask UpdateHeadersAsync(
        string name, AuthorizationHeaders? expiredHeaders,
        AuthorizationHeaders? newHeaders, CancellationToken ct,
        string? cacheKeySuffix = null)
    {
        // Log or transform headers between cache and auth handler
        return default;
    }
}
```

The interceptor chain becomes: `MemoryCache → MyLoggingInterceptor → AuthHandler → MyLoggingInterceptor → MemoryCache`. Build your own cache backend by targeting [AuthorizationInterceptor.Extensions.Abstractions](https://www.nuget.org/packages/AuthorizationInterceptor.Extensions.Abstractions).

## Sample Applications

Run a working demo with a mock API endpoint:

```
cd samples
dotnet run --project TargetApi   # starts mock auth server on :5001
# in another terminal:
dotnet run --project SourceApi   # calls the mock API with interceptor enabled
```

Source: [Samples](./samples/README.md)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
