![AuthorizationInterceptor Icon](./resources/icon.png)

# Authorization Interceptor

A lightweight .NET library that keeps your `HttpClient` authenticated for you. When a request comes back `401 Unauthorized`, the interceptor re-authenticates, refreshes the headers, and retries the request — automatically. No manual token juggling.

[![GitHub Actions](https://github.com/Adolfok3/authorizationinterceptor/actions/workflows/main.yml/badge.svg)](https://github.com/Adolfok3/AuthorizationInterceptor/actions)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](./LICENSE)
[![Codecov](https://codecov.io/github/Adolfok3/AuthorizationInterceptor/graph/badge.svg?token=PHBV20RCQK)](https://codecov.io/github/Adolfok3/AuthorizationInterceptor)
[![NuGet Version](https://img.shields.io/nuget/vpre/AuthorizationInterceptor)](https://www.nuget.org/packages/AuthorizationInterceptor)
[![.NET Support](https://img.shields.io/badge/.NET-8%2C9%2C10-blue)](https://dotnet.microsoft.com/download)

## Quick Start

Only two steps are required: tell the interceptor **how to authenticate**, then **attach it to an `HttpClient`**.

**1. Install the package**

```bash
dotnet add package AuthorizationInterceptor
```

**2. Write a handler that returns your authorization headers**

`AuthorizationHeaders` is just a string dictionary — return whatever headers your API needs.

```csharp
public class TargetApiAuth : IAuthenticationHandler
{
    private readonly HttpClient _client;

    public TargetApiAuth(IHttpClientFactory factory)
        => _client = factory.CreateClient("Auth");

    public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(
        AuthorizationHeaders? expiredHeaders, CancellationToken ct)
    {
        var response = await _client.PostAsync("auth", content: null, ct);
        var token = await response.Content.ReadAsStringAsync(ct);

        return new AuthorizationHeaders
        {
            ["Authorization"] = $"Bearer {token}"
        };
    }
}
```

**3. Attach it to your `HttpClient`**

```csharp
builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuth>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://targetapi.com"));
```

**That's it.** Every call made through this `HttpClient` now carries the authorization headers, and any `401` triggers a re-authentication and retry — transparently.

> `AuthenticateAsync` is called the first time headers are needed and again whenever a request is rejected. On a rejection, the previously used headers are passed back in as `expiredHeaders` (see [OAuth & refresh tokens](#oauth--refresh-tokens) below).

## Features

- **Automatic retry on auth failure** — intercepts `401` responses and retries with fresh headers.
- **Any headers you want** — return a simple dictionary, or use the built-in OAuth2 helper.
- **OAuth2 refresh tokens** — reuse existing tokens through the `RefreshToken` flow.
- **Caching** — in-memory, distributed (Redis/NCache), or hybrid, to avoid redundant logins.
- **Deduplicated authentication** — concurrent requests share a single authentication call.
- **Local & distributed locking** — coalesce authentication within an instance, and optionally across instances.
- **.NET 8, 9, and 10.**

## OAuth & refresh tokens

If your API issues OAuth2 tokens, return `OAuthHeaders` instead of a raw dictionary. The interceptor then knows the token's lifetime (so it can cache and expire it) and hands the expired headers back to you so you can refresh instead of logging in again.

```csharp
public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(
    AuthorizationHeaders? expiredHeaders, CancellationToken ct)
{
    // expiredHeaders is null on the first authentication, and populated on a refresh.
    var response = expiredHeaders?.OAuthHeaders?.RefreshToken is { } refreshToken
        ? await _client.PostAsync($"refresh?refresh={refreshToken}", content: null, ct)
        : await _client.PostAsync("auth", content: null, ct);

    var json = await response.Content.ReadAsStringAsync(ct);
    var tokens = JsonSerializer.Deserialize<UserTokens>(json)!;

    // (AccessToken, TokenType, ExpiresIn, RefreshToken, ExpiresInRefreshToken)
    return new OAuthHeaders(
        tokens.AccessToken, tokens.TokenType, tokens.ExpiresIn,
        tokens.RefreshToken, tokens.RefreshAccessTokenExpiresIn);
}

public record UserTokens(
    string AccessToken, string TokenType, int ExpiresIn,
    string RefreshToken, int RefreshAccessTokenExpiresIn);
```

`OAuthHeaders` becomes a standard `Authorization: {TokenType} {AccessToken}` header automatically. Only `AccessToken` and `TokenType` are required; the rest are optional. The refresh branch is only needed if your provider supports refresh tokens — otherwise just re-authenticate.

## Caching (recommended for production)

Without a cache, a fresh token is requested on every expiration. Add one cache interceptor and tokens are reused until they expire.

| Package | Use case |
| --- | --- |
| [AuthorizationInterceptor.Extensions.MemoryCache](https://www.nuget.org/packages/AuthorizationInterceptor.Extensions.MemoryCache) | Single-instance apps |
| [AuthorizationInterceptor.Extensions.DistributedCache](https://www.nuget.org/packages/AuthorizationInterceptor.Extensions.DistributedCache) | Multi-instance (Redis, NCache, …) |
| [AuthorizationInterceptor.Extensions.HybridCache](https://www.nuget.org/packages/AuthorizationInterceptor.Extensions.HybridCache) | Memory + distributed — **recommended** |

Enable it in the options callback — e.g. hybrid caching:

```csharp
builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
    {
        options.UseHybridCacheInterceptor();
    })
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://targetapi.com"));
```

Hybrid caching checks in-memory first (fastest), falls back to the distributed cache (shared across instances), and only then calls your handler. Swap in `UseMemoryCacheInterceptor()` or `UseDistributedCacheInterceptor()` if you prefer one layer.

## Advanced

<details open>
<summary><strong>Concurrency &amp; deduplication</strong></summary>

When several requests need headers at the same time and none are cached, only one of them calls your handler. The others wait and reuse the result. The same applies after a `401`: a request only re-authenticates if no one else has already replaced the rejected token.

This deduplication is enabled with a **local lock** (`AuthenticationLockMode.Local`), scoped to the process and to the `HttpClient` name (plus any `CacheKeyBuilder` suffix). Locking is disabled by default (`AuthenticationLockMode.None`). Across instances it's the shared cache — not a lock — that keeps logins down; with a cold distributed cache two instances can still authenticate at once. If your provider invalidates the previous token on every issuance, add a [distributed lock](#locking-across-instances).

Deduplication requires at least one cache interceptor. Without one there is nothing to share, so every request authenticates on its own.

</details>

<details open>
<summary><strong>Locking across instances</strong></summary>

This is what protects you from a **cache stampede**: when the shared token expires (or the cache is cold) and many instances suddenly hit the API at once, without a distributed lock they would all authenticate simultaneously, hammering the auth provider with duplicate logins. A distributed lock lets a single instance authenticate while the others wait and reuse its result.

`LockMode` controls how concurrent authentications are serialized. It's a `[Flags]` enum, so scopes combine:

| Mode | Prevents concurrent authentication… |
| --- | --- |
| `AuthenticationLockMode.None` | not at all (default) |
| `AuthenticationLockMode.Local` | within a single instance |
| `AuthenticationLockMode.Distributed` | across multiple instances |
| `Local \| Distributed` | both (recommended when scaling out) |

The distributed lock builds on [DistributedLock.Core](https://www.nuget.org/packages/DistributedLock.Core), which is only the abstraction — you choose and register the provider (Redis, SQL Server, Postgres, Azure, FileSystem, …):

```bash
dotnet add package DistributedLock.Redis
```

```csharp
using Medallion.Threading;
using Medallion.Threading.Redis;
using StackExchange.Redis;

var multiplexer = ConnectionMultiplexer.Connect("localhost:6379");
builder.Services.AddSingleton<IDistributedLockProvider>(_ =>
    new RedisDistributedSynchronizationProvider(multiplexer.GetDatabase()));

builder.Services.AddHttpClient("TargetApi")
    .AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
    {
        options.UseHybridCacheInterceptor();
        options.LockMode = AuthenticationLockMode.Local | AuthenticationLockMode.Distributed;
        options.DistributedLockTimeout = TimeSpan.FromSeconds(30); // optional; null waits indefinitely
    });
```

With the distributed lock enabled, only one instance authenticates for a given key at a time. After acquiring the lock it re-checks the shared cache (double-checked locking): if another instance already refreshed the headers, it adopts them and skips the login. Pair it with a distributed (or hybrid) cache so there's a shared cache for that re-check to hit.

> If `LockMode` includes `Distributed` but no `IDistributedLockProvider` is registered, creating the `HttpClient` throws `InvalidOperationException`.

</details>

<details open>
<summary><strong>Customizing what counts as "unauthorized"</strong></summary>

By default a response only triggers re-authentication when its status code is `401 Unauthorized`. But `UnauthenticatedPredicate` is a full `Func<HttpResponseMessage, bool>`, so you decide what "unauthorized" means for your API — it isn't limited to status codes. You get the whole response, so you can inspect the status, a header, or even the response body.

Most common case — also treat `403` as expired:

```csharp
.AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
{
    options.UnauthenticatedPredicate = response =>
        response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized;
});
```

Some APIs answer `200 OK` with an error in a header or in the payload. You can key off those instead:

```csharp
// Based on a custom header
options.UnauthenticatedPredicate = response =>
    response.Headers.TryGetValues("x-auth-status", out var values)
        && values.Contains("expired");

// Based on the response body
options.UnauthenticatedPredicate = response =>
{
    var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    return body.Contains("token_expired");
};
```

Return `true` for any response that means "the credentials are no longer valid" and the interceptor will re-authenticate and retry.

</details>

<details open>
<summary><strong>Passing extra dependencies into a handler</strong></summary>

Use the delegate overload when your handler needs values that aren't in DI:

```csharp
.AddAuthorizationInterceptorHandler(sp =>
    ActivatorUtilities.CreateInstance<TargetApiAuth>(sp, someOtherDependency));
```

</details>

<details open>
<summary><strong>Per-request cache keys</strong></summary>

When one `HttpClient` authenticates on behalf of different users, tenants, stores, etc., use `CacheKeyBuilder` to cache their headers separately. The returned value is appended to the cache key.

```csharp
.AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
{
    options.UseHybridCacheInterceptor();
    options.CacheKeyBuilder = accessor =>
        accessor.HttpContext?.User.FindFirst("sub")?.Value;
});
```

Or from a route/query value:

```csharp
options.CacheKeyBuilder = accessor =>
{
    var http = accessor.HttpContext;
    var storeId = http?.Request.RouteValues["storeId"]?.ToString()
        ?? http?.Request.Query["storeId"].ToString();
    return string.IsNullOrWhiteSpace(storeId) ? null : storeId;
};
```

Requests with different values no longer share cached headers. If the builder returns `null`/empty, the default key (the `HttpClient` name) is used. Interceptors receive the two halves combined as a single `key`: just the name when no suffix applies, or `{name}_{suffix}` when one does.

</details>

<details open>
<summary><strong>Custom interceptors</strong></summary>

Add your own steps to the interceptor chain — e.g. logging or a custom cache backend:

```csharp
.AddAuthorizationInterceptorHandler<TargetApiAuth>(options =>
{
    options.UseMemoryCacheInterceptor();
    options.UseCustomInterceptor<MyLoggingInterceptor>();
});
```

```csharp
public class MyLoggingInterceptor : IAuthorizationInterceptor
{
    public ValueTask<AuthorizationHeaders?> GetHeadersAsync(
        string key, CancellationToken ct)
        => new(new AuthorizationHeaders());

    public ValueTask UpdateHeadersAsync(
        string key, AuthorizationHeaders? expiredHeaders,
        AuthorizationHeaders? newHeaders, CancellationToken ct)
    {
        // Log or transform headers between cache and auth handler
        return default;
    }
}
```

The chain becomes `MemoryCache → MyLoggingInterceptor → AuthHandler → MyLoggingInterceptor → MemoryCache`. Build a custom cache backend by targeting [AuthorizationInterceptor.Extensions.Abstractions](https://www.nuget.org/packages/AuthorizationInterceptor.Extensions.Abstractions).

</details>

## Sample Applications

Run a working demo with a mock API endpoint:

```bash
cd samples
dotnet run --project TargetApi   # starts mock auth server on :5001
# in another terminal:
dotnet run --project SourceApi   # calls the mock API with interceptor enabled
```

Source: [Samples](./samples/README.md)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
