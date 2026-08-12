using AuthorizationInterceptor.Extensions.Abstractions.Headers;

namespace AuthorizationInterceptor.Utils;

internal readonly record struct AuthenticationLockResult(AuthorizationHeaders? Headers, bool Joined);