# Changelog

All notable changes to this project will be documented in this file.

## [6.3.0] - 2026-08-20

### Added

- Added a built-in OAuth 2.0 Client Credentials authentication handler through `AddClientCredentialsAuthorizationInterceptorHandler`.
- Added support for `client_secret_basic` (default), `client_secret_post`, optional scopes, additional token parameters, and a dedicated named `HttpClient` for token requests.

## [6.2.0] - 2026-08-11

### Added

- Added `AuthorizationInterceptorOptions.LockMode`, an `AuthenticationLockMode` flags enum that controls how concurrent authentications are serialized:
  - `Local` — single-flight lock that coalesces concurrent authentications **within the instance**.
  - `Distributed` — distributed lock that serializes authentication **across multiple instances**, so only one instance authenticates for a given key at a time.
  - `None` (default) — no locking.
  - `Local` and `Distributed` are independent flags and can be combined (`Local | Distributed`).
- Added distributed locking backed by [DistributedLock.Core](https://www.nuget.org/packages/DistributedLock.Core). When `LockMode` includes `Distributed`, register an `IDistributedLockProvider` of your choice (Redis, SqlServer, Postgres, Azure, FileSystem, etc.) in the service collection. After acquiring the lock, the interceptor re-checks the shared cache (double-checked locking) and adopts headers a concurrent instance may have already refreshed, skipping a redundant authentication.
- Added `AuthorizationInterceptorOptions.DistributedLockTimeout`, the maximum time to wait to acquire the distributed lock. Defaults to `null` (wait indefinitely, respecting cancellation).

### Changed

- The abstractions package now depends on `DistributedLock.Core` to expose the `IDistributedLockProvider` abstraction used by `LockMode.Distributed`. The concrete provider (and its package) is chosen and registered by the application.
- Enabling `LockMode.Distributed` without an `IDistributedLockProvider` registered throws `InvalidOperationException` when the `HttpClient` handler is created, with guidance to register a provider.

## [6.1.0] - 2026-08-10

### Fixed

- Interceptor dependencies passed to `UseCustomInterceptor` / `UseMemoryCacheInterceptor` are now registered at registration time. They used to be registered inside the `HttpClient` handler factory, which runs after the service provider is built: the interceptor was activated before the callback ran, and the callback itself threw `InvalidOperationException: The service collection cannot be modified because it is read-only` on any new registration.
- Concurrent requests that find no valid headers now perform a single authentication per cache key, instead of one authentication per in-flight request. Callers that had to wait re-check the interceptors and reuse whatever the winner cached.
- After an unauthenticated response, a caller no longer re-authenticates if another caller already replaced the rejected headers, determined by comparing `AuthorizationHeaders.AuthenticatedAt`.

### Changed

- The `HttpClient` name and the `CacheKeyBuilder` suffix are now combined once, by the authorization handler, and travel through the interceptor chain as a single `key`. Interceptors no longer assemble the cache key themselves.
- Concurrent authentications are now coalesced rather than serialized: callers arriving while one is already running await its result instead of waiting for a lock and then re-reading the cache. This removes one cache round-trip per waiting caller.
- A shared authentication runs detached from any single caller's `CancellationToken`, so a caller that cancels no longer aborts the authentication the others are waiting for. Consequently, the `IAuthenticationHandler` of a coalesced authentication receives `CancellationToken.None`. Callers still observe their own token while waiting, but the exception they get is now `TaskCanceledException` instead of `OperationCanceledException`; code catching `OperationCanceledException` is unaffected, since the former derives from the latter.

### Breaking Changes

- `IAuthorizationInterceptor.GetHeadersAsync` and `IAuthorizationInterceptor.UpdateHeadersAsync` no longer take a `cacheKeySuffix` parameter, and their first parameter is now the combined `key` rather than the `HttpClient` name. Custom interceptor implementations must update their signatures:

  ```csharp
  // before
  ValueTask<AuthorizationHeaders?> GetHeadersAsync(string name, CancellationToken ct, string? cacheKeySuffix = null);
  ValueTask UpdateHeadersAsync(string name, AuthorizationHeaders? expired, AuthorizationHeaders? updated, CancellationToken ct, string? cacheKeySuffix = null);

  // after
  ValueTask<AuthorizationHeaders?> GetHeadersAsync(string key, CancellationToken ct);
  ValueTask UpdateHeadersAsync(string key, AuthorizationHeaders? expired, AuthorizationHeaders? updated, CancellationToken ct);
  ```

  Implementations that combined `name` and `cacheKeySuffix` themselves should now use `key` directly. Cache keys produced by the bundled MemoryCache, DistributedCache, and HybridCache interceptors are byte-identical to 6.x, so cached authorization headers survive the upgrade.

## [6.0.1] - 2026-06-16

### Fixed

- Resolved `IHttpContextAccessor` directly in the authorization handler instead of creating a runtime service scope for cache key building.
- Registered `IHttpContextAccessor` for all `AddAuthorizationInterceptorHandler` overloads so `CacheKeyBuilder` works consistently.

## [6.0.0] - 2026-06-15

### Added

- Added `AuthorizationInterceptorOptions.CacheKeyBuilder`, allowing applications to build a cache key suffix from the current HTTP context.
- Added cache key suffix support across the authorization handler, strategy, memory cache, distributed cache, hybrid cache, and custom interceptor flow.
- Added sample coverage for per-request cache key partitioning using propagated request headers.
- Added debug logs that include the integration name and cache key suffix when headers are added, skipped, or refreshed after an unauthenticated response.

### Changed

- Updated cache interceptors to isolate authorization headers by `HttpClient` name plus the optional cache key suffix.
- Updated the authorization strategy APIs to pass the cache key suffix through header lookup and refresh operations.
- Updated package targets to .NET 8, .NET 9, and .NET 10.
- Updated the .NET 10 `Microsoft.Extensions.Http` dependency used by the abstractions package.
- Improved README documentation with a clearer quick start, cache guidance, customization examples, and sample application instructions.

### Breaking Changes

- Removed .NET 6 and .NET 7 target framework support. Consumers must target .NET 8 or later.
- Changed `IAuthorizationInterceptor.GetHeadersAsync` and `IAuthorizationInterceptor.UpdateHeadersAsync` to receive an optional `cacheKeySuffix` parameter. Custom interceptor implementations must update their method signatures.
- The abstractions package now depends on `Microsoft.AspNetCore.Http` so `CacheKeyBuilder` can use `IHttpContextAccessor`.
