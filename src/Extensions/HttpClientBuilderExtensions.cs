using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Options;
using AuthorizationInterceptor.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthorizationInterceptor.Extensions
{
    /// <summary>
    /// Extension methods that add a new authorization interceptor handler configuration for <see cref="IHttpClientBuilder"/>
    /// </summary>
    public static class HttpClientBuilderExtensions
    {
        /// <summary>
        /// Init a new authorization interceptor handler configuration for IHttpClientBuilder
        /// </summary>
        /// <typeparam name="T">Implementation of <see cref="IAuthenticationHandler"/></typeparam>
        /// <param name="builder"><see cref="IHttpClientBuilder"/></param>
        /// <param name="options">Configuration options of Authorization interceptor</param>
        /// <returns>Returns <see cref="IHttpClientBuilder"/></returns>
        public static IHttpClientBuilder AddAuthorizationInterceptorHandler<T>(this IHttpClientBuilder builder, Action<AuthorizationInterceptorOptions>? options = null)
            where T : class, IAuthenticationHandler
        {
            options ??= (options => new AuthorizationInterceptorOptions());
            var optionsInstance = new AuthorizationInterceptorOptions();
            options.Invoke(optionsInstance);

            AddInterceptorsDependencies(builder, optionsInstance._interceptors.Select(s => s.Item2).ToList());
            builder.AddHttpMessageHandler(provider => new AuthorizationInterceptorHandler(optionsInstance, new AuthorizationInterceptorStrategy((IAuthenticationHandler)ActivatorUtilities.CreateInstance(provider, typeof(T)), provider.GetRequiredService<ILoggerFactory>(), CreateInterceptorsIntances(provider, optionsInstance._interceptors)), provider.GetRequiredService<ILoggerFactory>()));

            return builder;
        }

        private static void AddInterceptorsDependencies(IHttpClientBuilder builder, List<Func<IServiceCollection, IServiceCollection>?> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                dependency?.Invoke(builder.Services);
            }
        }

        private static IAuthorizationInterceptor[] CreateInterceptorsIntances(IServiceProvider provider, List<(Type, Func<IServiceCollection, IServiceCollection>?)> interceptors)
        {
            List<IAuthorizationInterceptor> interceptorsInstances = new();

            foreach (var interceptor in interceptors)
            {
                interceptorsInstances.Add((IAuthorizationInterceptor)ActivatorUtilities.CreateInstance(provider, interceptor.Item1));
            }

            return interceptorsInstances.ToArray();
        }
    }
}

