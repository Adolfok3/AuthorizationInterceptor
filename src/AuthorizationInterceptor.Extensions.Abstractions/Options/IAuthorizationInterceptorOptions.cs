using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AuthorizationInterceptor.Extensions.Abstractions.Options
{
    /// <summary>
    /// Options class that configures the authorization interceptors
    /// </summary>
    public interface IAuthorizationInterceptorOptions
    {
        /// <summary>
        /// Adds a custom interceptor to the interceptor sequence. Note that the interceptor addition sequence interferes with the headers query sequence.
        /// </summary>
        /// <typeparam name="T">Implementation class of type <see cref="IAuthorizationInterceptor"/></typeparam>
        /// <param name="func">Access to <see cref="IServiceCollection"/> if necessary</param>
        void UseCustomInterceptor<T>(Func<IServiceCollection, IServiceCollection>? func = null) where T : IAuthorizationInterceptor;
    }
}
