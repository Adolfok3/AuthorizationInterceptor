# Changelog

All notable changes to this project will be documented in this file.

## [7.0.0] - 2026-07-21

### Fixed

- Interceptor dependencies passed to `UseCustomInterceptor` / `UseMemoryCacheInterceptor` are now registered at registration time. They used to be registered inside the `HttpClient` handler factory, which runs after the service provider is built: the interceptor was activated before the callback ran, and the callback itself threw `InvalidOperationException: The service collection cannot be modified because it is read-only` on any new registration.
- Concurrent requests that find no valid headers now perform a single authentication per cache key, instead of one authentication per in-flight request. Callers that had to wait re-check the interceptors and reuse whatever the winner cached.
- After an unauthenticated response, a caller no longer re-authenticates if another caller already replaced the rejected headers, determined by comparing `AuthorizationHeaders.AuthenticatedAt`.

### Changed

- The `HttpClient` name and the `CacheKeyBuilder` suffix are now combined once, by the authorization handler, and travel through the interceptor chain as a single `key`. Interceptors no longer assemble the cache key themselves.

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
