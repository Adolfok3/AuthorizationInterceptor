using System;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using AuthorizationInterceptor.Extensions.MemoryCache.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.Extensions.MemoryCache.Extensions
{
    /// <summary>
    /// Extension methods that Configures the authorization interceptor to use a memory cache interceptor for <see cref="IAuthorizationInterceptorOptions"/>
    /// </summary>
    public static class AuthorizationInterceptorOptionsExtensions
    {
        /// <summary>
        /// Configures the authorization interceptor to use a memory cache interceptor.
        /// </summary>
        /// <param name="options"><see cref="IAuthorizationInterceptorOptions"/></param>
        /// <param name="servicesFunc"><see cref="IServiceCollection"/> to configure additional dependencies if necessary</param>
        /// <returns><see cref="IAuthorizationInterceptorOptions"/></returns>
        public static IAuthorizationInterceptorOptions UseMemoryCacheInterceptor(this IAuthorizationInterceptorOptions options, Func<IServiceCollection, IServiceCollection>? servicesFunc = null)
        {
            options.UseCustomInterceptor<MemoryCacheInterceptor>(servicesFunc);
            return options;
        }
    }
}
