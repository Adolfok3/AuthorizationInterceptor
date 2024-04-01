using AuthorizationInterceptor.Builder;
using AuthorizationInterceptor.Extensions.Memory;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Options;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AuthorizationInterceptor.Extensions
{
    /// <summary>
    /// Extension methods that add a new authorization interceptor handler configuration for <see cref="IHttpClientBuilder"/>
    /// </summary>
    public static class HttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a new authorization interceptor handler configuration for IHttpClientBuilder
        /// </summary>
        /// <typeparam name="T">Implementation of <see cref="IAuthenticationHandler"/></typeparam>
        /// <param name="builder"><see cref="IHttpClientBuilder"/></param>
        /// <returns>Returns a new instance of <see cref="IAuthorizationInterceptorBuilder"/></returns>
        public static IAuthorizationInterceptorBuilder AddAuthorizationInterceptorHandler<T>(this IHttpClientBuilder builder)
            where T : class, IAuthenticationHandler
        {
            return AddAuthorizationInterceptorHandler<T>(builder, opt => new AuthorizationInterceptorOptions());
        }

        /// <summary>
        /// Init a new authorization interceptor handler configuration for IHttpClientBuilder
        /// </summary>
        /// <typeparam name="T">Implementation of <see cref="IAuthenticationHandler"/></typeparam>
        /// <param name="builder"><see cref="IHttpClientBuilder"/></param>
        /// <param name="options">Configuration options of Authorization interceptor</param>
        /// <returns>Returns a new instance of <see cref="IAuthorizationInterceptorBuilder"/></returns>
        public static IAuthorizationInterceptorBuilder AddAuthorizationInterceptorHandler<T>(this IHttpClientBuilder builder, Action<AuthorizationInterceptorOptions>? options = null)
            where T : class, IAuthenticationHandler
        {
            options = options ?? (options => new AuthorizationInterceptorOptions());
            var optionsInstance = new AuthorizationInterceptorOptions();
            options.Invoke(optionsInstance);
            var authorizationBuilder = new AuthorizationInterceptorBuilder(builder, typeof(T), optionsInstance);

            if (optionsInstance.DisableMemoryCache)
                return authorizationBuilder;

            authorizationBuilder.AddMemoryCache(optionsInstance.MemoryCacheOptions);
            return authorizationBuilder;
        }
    }
}

