using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.DistributedCache.Interceptors;

namespace AuthorizationInterceptor.Extensions.DistributedCache.Extensions;

/// <summary>
/// Extension methods that Configures the authorization interceptor to use a distributed cache interceptor for <see cref="IAuthorizationInterceptorOptions"/>
/// </summary>
public static class AuthorizationInterceptorOptionsExtensions
{
    /// <summary>
    /// Configures the authorization interceptor to use a distributed cache interceptor.
    /// </summary>
    /// <param name="options"><see cref="IAuthorizationInterceptorOptions"/></param>
    /// <returns><see cref="IAuthorizationInterceptorOptions"/></returns>
    public static IAuthorizationInterceptorOptions UseDistributedCacheInterceptor(this IAuthorizationInterceptorOptions options)
    {
        options.UseCustomInterceptor<DistributedCacheAuthorizationInterceptor>();
        return options;
    }
}
