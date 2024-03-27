using AuthorizationInterceptor.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.Builder
{
    /// <summary>
    /// Provides a mechanism for configuring authorization interceptor components.
    /// </summary>
    public interface IAuthorizationInterceptorBuilder
    {
        /// <summary>
        /// The <see cref="IHttpClientBuilder"/> that contains a authorization interceptor configuration
        /// </summary>
        public IHttpClientBuilder HttpClientBuilder { get; }

        /// <summary>
        /// Adds a interceptor of type <see cref="AuthorizationInterceptorBase"/> to the authorization interceptors.
        /// </summary>
        /// <typeparam name="T">Implementation of <see cref="AuthorizationInterceptorBase"/></typeparam>
        /// <returns><see cref="IAuthorizationInterceptorBuilder"/></returns>
        IAuthorizationInterceptorBuilder AddCustom<T>() where T : AuthorizationInterceptorBase;

        /// <summary>
        /// Builds and configures the authorization interceptor components for an HTTP client.
        /// </summary>
        /// <returns><see cref="IHttpClientBuilder"/></returns>
        IHttpClientBuilder BuildAuthorizationInterceptor();
    }
}
