using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.HybridCache.Interceptors;

namespace AuthorizationInterceptor.Extensions.HybridCache.Extensions;

/// <summary>
/// Extension methods that Configures the authorization interceptor to use a hybrid cache interceptor for <see cref="IAuthorizationInterceptorOptions"/>
/// </summary>
public static class AuthorizationInterceptorOptionsExtensions
{
    /// <summary>
    /// Configures the authorization interceptor to use a hybrid cache interceptor.
    /// </summary>
    /// <param name="options"><see cref="IAuthorizationInterceptorOptions"/></param>
    /// <returns><see cref="IAuthorizationInterceptorOptions"/></returns>
    public static IAuthorizationInterceptorOptions UseHybridCacheInterceptor(this IAuthorizationInterceptorOptions options)
    {
        options.UseCustomInterceptor<HybridCacheAuthorizationInterceptor>();
        return options;
    }
}