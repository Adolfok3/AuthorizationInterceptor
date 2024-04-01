using AuthorizationInterceptor.Builder;
using AuthorizationInterceptor.Interceptors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AuthorizationInterceptor.Extensions.Memory
{
    /// <summary>
    /// Extension methods that Configures the authorization interceptor to use an in-memory cache interceptor for <see cref="IAuthorizationInterceptorBuilder"/>
    /// </summary>
    public static class MemoryAuthorizationInterceptorBuilderExtensions
    {
        /// <summary>
        /// Configures the authorization interceptor to use an in-memory cache interceptor.
        /// </summary>
        /// <param name="builder"><see cref="IAuthorizationInterceptorBuilder"/></param>
        /// <param name="options"><see cref="MemoryCacheOptions"/></param>
        /// <returns><see cref="IAuthorizationInterceptorBuilder"/></returns>
        public static IAuthorizationInterceptorBuilder AddMemoryCache(this IAuthorizationInterceptorBuilder builder, Action<MemoryCacheOptions>? options = null)
        {
            options = options ?? (options => new MemoryCacheOptions());
            builder.AddCustom<MemoryAuthorizationInterceptor>();
            builder.HttpClientBuilder.Services.AddMemoryCache(options);
            return builder;
        }
    }
}
