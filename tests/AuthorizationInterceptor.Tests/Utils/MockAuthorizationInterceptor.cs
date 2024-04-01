using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Interceptors;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AuthorizationInterceptor.Tests.Utils
{
    public class MockAuthorizationInterceptor : AuthorizationInterceptorBase
    {
        public MockAuthorizationInterceptor(IAuthenticationHandler authentication, ILogger<AuthorizationInterceptorBase> logger, IAuthorizationInterceptor? next = null) : base("MOCK", authentication, logger, next) { }
        public MockAuthorizationInterceptor() : base("MOCK", Substitute.For<IAuthenticationHandler>(), Substitute.For<ILogger<AuthorizationInterceptorBase>>(), Substitute.For<IAuthorizationInterceptor>()) { }
    }

    public class Mock2AuthorizationInterceptor : AuthorizationInterceptorBase
    {
        public Mock2AuthorizationInterceptor() : base("MOCK", Substitute.For<IAuthenticationHandler>(), Substitute.For<ILogger<AuthorizationInterceptorBase>>(), Substitute.For<IAuthorizationInterceptor>()) { }
    }

    public class Mock3AuthorizationInterceptor : AuthorizationInterceptorBase
    {
        public Mock3AuthorizationInterceptor() : base("MOCK", Substitute.For<IAuthenticationHandler>(), Substitute.For<ILogger<AuthorizationInterceptorBase>>(), Substitute.For<IAuthorizationInterceptor>()) { }
    }
}
