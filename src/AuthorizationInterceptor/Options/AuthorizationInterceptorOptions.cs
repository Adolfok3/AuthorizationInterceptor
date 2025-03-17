using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace AuthorizationInterceptor.Options;

/// <summary>
/// Options class that configures the authorization interceptors
/// </summary>
public class AuthorizationInterceptorOptions : IAuthorizationInterceptorOptions
{
    internal readonly List<(Type, Func<IServiceCollection, IServiceCollection>?)> Interceptors = [];

    /// <summary>
    /// Defines a predicate to know when the request was unauthenticated. If this happens, a new authorization header will be generated. Default is response with <see cref="HttpStatusCode.Unauthorized"/>.
    /// </summary>
    public Func<HttpResponseMessage, bool> UnauthenticatedPredicate { get; set; } = (response) => response.StatusCode == HttpStatusCode.Unauthorized;

    /// <summary>
    /// Adds a custom interceptor to the interceptor sequence. Note that the interceptor addition sequence interferes with the headers query sequence.
    /// </summary>
    /// <typeparam name="T">Implementation class of type <see cref="IAuthorizationInterceptor"/></typeparam>
    /// <param name="func">Access to <see cref="IServiceCollection"/> if necessary inject some dependencies</param>
    public void UseCustomInterceptor<T>(Func<IServiceCollection, IServiceCollection>? func = null) where T : IAuthorizationInterceptor
        => Interceptors.Add((typeof(T), func));
}

