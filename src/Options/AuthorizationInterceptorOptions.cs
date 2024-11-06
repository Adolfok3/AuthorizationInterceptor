using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace AuthorizationInterceptor.Options
{
    /// <summary>
    /// Options class that configures the authorization interceptors
    /// </summary>
    public class AuthorizationInterceptorOptions : IAuthorizationInterceptorOptions
    {
        internal readonly List<(ServiceDescriptor, Func<IServiceCollection, IServiceCollection>?)> Interceptors = new();

        /// <summary>
        /// Defines a predicate to know when the request was unauthenticated. If this happens, a new authorization header will be generated. Default is response with <see cref="HttpStatusCode.Unauthorized"/>.
        /// </summary>
        public Func<HttpResponseMessage, bool> UnauthenticatedPredicate { get; set; } = (response) => response.StatusCode == HttpStatusCode.Unauthorized;

        /// <summary>
        /// Adds a custom interceptor to the interceptor sequence. Note that the interceptor addition sequence interferes with the headers query sequence.
        /// </summary>
        /// <typeparam name="T">Implementation class of type <see cref="IAuthorizationInterceptor"/></typeparam>
        /// <param name="func">Access to <see cref="IServiceCollection"/> if necessary</param>
        public void UseCustomInterceptor<T>(Func<IServiceCollection, IServiceCollection>? func = null) where T : IAuthorizationInterceptor
        {
            Interceptors.Add((new ServiceDescriptor(typeof(IAuthorizationInterceptor), typeof(T), ServiceLifetime.Transient), func));
        }
    }
}

