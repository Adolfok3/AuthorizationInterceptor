using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Interceptors;
using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Options;
using AuthorizationInterceptor.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Extensions;

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
        builder.AddHttpMessageHandler(provider => new AuthorizationInterceptorHandler(
            builder.Name,
            optionsInstance.UnauthenticatedPredicate,
            CreateAuthenticationHandler<T>(provider),
            CreateStrategy(provider, builder, optionsInstance.Interceptors),
            provider.GetRequiredService<ILoggerFactory>()
        ));

        return builder;
    }

    /// <summary>
    /// Init a new authorization interceptor handler configuration for IHttpClientBuilder
    /// </summary>
    /// <param name="builder"><see cref="IHttpClientBuilder"/></param>
    /// <param name="authHandlerImpl">A delegate that provides an implementation of <see cref="IAuthenticationHandler"/> using the given <see cref="IServiceProvider"/>.</param>
    /// <param name="options">Configuration options of Authorization interceptor.</param>
    /// <returns>Returns <see cref="IHttpClientBuilder"/></returns>
    public static IHttpClientBuilder AddAuthorizationInterceptorHandler(this IHttpClientBuilder builder, Func<IServiceProvider, IAuthenticationHandler> authHandlerImpl, Action<AuthorizationInterceptorOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(authHandlerImpl);

        var optionsInstance = RequireOptions(options);
        builder.AddHttpMessageHandler(provider => new AuthorizationInterceptorHandler(
            builder.Name,
            optionsInstance.UnauthenticatedPredicate,
            authHandlerImpl.Invoke(provider),
            CreateStrategy(provider, builder, optionsInstance.Interceptors),
            provider.GetRequiredService<ILoggerFactory>()
        ));

        return builder;
    }

    private static AuthorizationInterceptorOptions RequireOptions(Action<AuthorizationInterceptorOptions>? options)
    {
        var optionsInstance = new AuthorizationInterceptorOptions();
        options?.Invoke(optionsInstance);

        return optionsInstance;
    }

    private static T CreateAuthenticationHandler<T>(IServiceProvider provider) where T : class, IAuthenticationHandler
        => ActivatorUtilities.CreateInstance<T>(provider);

    private static AuthorizationInterceptorStrategy CreateStrategy(IServiceProvider provider, IHttpClientBuilder builder, List<(Type interceptor, Func<IServiceCollection, IServiceCollection>? dependencies)> interceptorsToBuild)
    {
        var interceptors = new IAuthorizationInterceptor[interceptorsToBuild.Count];

        for (int index = 0; index < interceptorsToBuild.Count; index++)
        {
            interceptors[index] = (IAuthorizationInterceptor)ActivatorUtilities.CreateInstance(provider, interceptorsToBuild[index].interceptor);
            interceptorsToBuild[index].dependencies?.Invoke(builder.Services);
        }

        return new AuthorizationInterceptorStrategy(provider.GetRequiredService<ILoggerFactory>(), interceptors);
    }
}

