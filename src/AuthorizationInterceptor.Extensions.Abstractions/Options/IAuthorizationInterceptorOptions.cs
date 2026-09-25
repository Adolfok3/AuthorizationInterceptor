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

    /// <summary>
    /// Defines how concurrent authentication attempts against the target API are serialized.
    /// <see cref="AuthenticationLockMode.Local"/> prevents concurrent authentication within a single instance (single-flight),
    /// while <see cref="AuthenticationLockMode.Distributed"/> prevents it across multiple instances.
    /// Default is <see cref="AuthenticationLockMode.None"/>.
    /// </summary>
    AuthenticationLockMode LockMode { get; set; }

    /// <summary>
    /// The maximum time to wait to acquire the distributed lock when <see cref="AuthenticationLockMode.Distributed"/> is enabled.
    /// If the lock cannot be acquired within this time, a <see cref="TimeoutException"/> is thrown.
    /// Default is <c>null</c>, which waits indefinitely (respecting cancellation).
    /// Ignored when <see cref="AuthenticationLockMode.Distributed"/> is not set.
    /// </summary>
    TimeSpan? DistributedLockTimeout { get; set; }
}
