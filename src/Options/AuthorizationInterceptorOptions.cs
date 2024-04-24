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
    /// <inheritdoc />
    /// </summary>
    public class AuthorizationInterceptorOptions : IAuthorizationInterceptorOptions
    {
        internal readonly List<(Type, Func<IServiceCollection, IServiceCollection>?)> _interceptors = new();

        /// <summary>
        /// Defines a predicate to know when the request was unauthenticated. If this happens, a new authorization header will be generated. Default is response with <see cref="HttpStatusCode.Unauthorized"/>.
        /// </summary>
        public Func<HttpResponseMessage, bool> UnauthenticatedPredicate { get; set; } = (response) => response.StatusCode == HttpStatusCode.Unauthorized;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public void UseCustomInterceptor<T>(Func<IServiceCollection, IServiceCollection>? func = null) where T : IAuthorizationInterceptor
        {
            _interceptors.Add((typeof(T), func));
        }
    }
}

