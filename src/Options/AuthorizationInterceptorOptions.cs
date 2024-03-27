using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net;
using System.Net.Http;

namespace AuthorizationInterceptor.Options
{
    /// <summary>
    /// Options class that configures the authorization interceptors
    /// </summary>
    public class AuthorizationInterceptorOptions
    {
        /// <summary>
        /// Defines a predicate to know when the request was unauthenticated. If this happens, a new authorization header will be generated. Default is response with <see cref="HttpStatusCode.Unauthorized"/>.
        /// </summary>
        public Func<HttpResponseMessage, bool> UnauthenticatedPredicate { get; set; } = (response) => response.StatusCode == HttpStatusCode.Unauthorized;

        /// <summary>
        /// Defines whether the authorization interceptor should use MemoryCache to maintain authorization headers.
        /// Recommended to keep this false to avoid unnecessary authentication requests.
        /// Default is false.
        /// </summary>
        public bool DisableMemoryCache { get; set; }

        /// <summary>
        /// Defines the MemoryCache options.
        /// </summary>
        public Action<MemoryCacheOptions>? MemoryCacheOptions { get; set; }
    }
}

