# Changelog

All notable changes to this project will be documented in this file.

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
