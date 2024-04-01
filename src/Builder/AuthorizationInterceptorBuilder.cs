using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Interceptors;
using AuthorizationInterceptor.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthorizationInterceptor.Builder
{
    /// <inheritdoc/>
    public class AuthorizationInterceptorBuilder : IAuthorizationInterceptorBuilder
    {
        /// <inheritdoc/>
        public IHttpClientBuilder HttpClientBuilder { get; }

        private readonly List<Type> _interceptors;
        private readonly Type _authenticationType;
        private readonly AuthorizationInterceptorOptions _options;

        public AuthorizationInterceptorBuilder(IHttpClientBuilder httpClientBuilder, Type authenticationType, AuthorizationInterceptorOptions options)
        {
            HttpClientBuilder = httpClientBuilder;
            _interceptors = new List<Type>();
            _authenticationType = authenticationType;
            _options = options;
        }

        /// <inheritdoc/>
        public IAuthorizationInterceptorBuilder AddCustom<T>() where T : AuthorizationInterceptorBase
        {
            _interceptors.Add(typeof(T));

            return this;
        }

        /// <inheritdoc/>
        public IHttpClientBuilder BuildAuthorizationInterceptor()
        {
            if (!_interceptors.Any())
                AddCustom<DefaultAuthorizationInterceptor>();

            HttpClientBuilder.AddHttpMessageHandler(provider => ActivatorUtilities.CreateInstance<AuthorizationInterceptorHandler>(provider, CreateInstance(provider, 0), _options));

            return HttpClientBuilder;
        }

        private object CreateInstance(IServiceProvider provider, int interceptorIndex)
        {
            if (interceptorIndex + 1 >= _interceptors.Count)
                return ActivatorUtilities.CreateInstance(provider, _interceptors[interceptorIndex], ActivatorUtilities.CreateInstance(provider, _authenticationType));

            return ActivatorUtilities.CreateInstance(provider, _interceptors[interceptorIndex], CreateInstance(provider, interceptorIndex + 1), ActivatorUtilities.CreateInstance(provider, _authenticationType));
        }
    }
}
