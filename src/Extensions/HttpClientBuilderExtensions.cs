using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Options;
using AuthorizationInterceptor.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            var optionsInstance = RequireOptions(options);
            AddInterceptorsDependencies(builder, optionsInstance._interceptors);
            builder.Services.TryAddTransient<IAuthorizationInterceptorStrategy, AuthorizationInterceptorStrategy>();
            builder.AddHttpMessageHandler(provider => ActivatorUtilities.CreateInstance<AuthorizationInterceptorHandler>(provider, builder.Name, optionsInstance.UnauthenticatedPredicate, ActivatorUtilities.CreateInstance(provider, typeof(T))));

            return builder;
        }

        private static AuthorizationInterceptorOptions RequireOptions(Action<AuthorizationInterceptorOptions>? options)
        {
            options ??= (options => new AuthorizationInterceptorOptions());
            var optionsInstance = new AuthorizationInterceptorOptions();
            options.Invoke(optionsInstance);

            return optionsInstance;
        }

        private static void AddInterceptorsDependencies(IHttpClientBuilder builder, List<(ServiceDescriptor serviceDescriptor, Func<IServiceCollection, IServiceCollection>? dependencies)> interceptors)
        {
            foreach (var interceptor in interceptors)
            {
                if (!builder.Services.Any(a => a.ServiceType == typeof(IAuthorizationInterceptor) && a.ImplementationType == interceptor.serviceDescriptor.ImplementationType))
                {
                    builder.Services.Add(interceptor.serviceDescriptor);
                    interceptor.dependencies?.Invoke(builder.Services);
                }
            }
        }
    }
}

