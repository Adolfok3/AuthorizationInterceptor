using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationInterceptor.Extensions.Abstractions.Options;

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

    /// <summary>
    /// Defines a function that builds the cache key suffix dynamically from the current HTTP context.
    /// When configured, cache entries will be differentiated per request (e.g., by user).
    /// The delegate receives an IHttpContextAccessor to access the current HttpContext via HttpContextAccessor.HttpContext
    /// and extract identifying information like userId, tenant, etc.
    /// If null or empty is returned, no additional suffix is applied.
    /// </summary>
    Func<IHttpContextAccessor, string?>? CacheKeyBuilder { get; set; }
}
