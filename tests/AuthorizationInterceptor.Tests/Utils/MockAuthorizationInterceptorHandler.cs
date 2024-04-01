using AuthorizationInterceptor.Handlers;
using AuthorizationInterceptor.Interceptors;
using AuthorizationInterceptor.Options;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Tests.Utils;

internal class MockAuthorizationInterceptorHandler : AuthorizationInterceptorHandler
{
    public MockAuthorizationInterceptorHandler(IAuthorizationInterceptor interceptor, AuthorizationInterceptorOptions options, ILogger<AuthorizationInterceptorHandler> logger) : base(interceptor, options, logger)
    {
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Any(a => a.Key == "ShouldReturnUnauthorized"))
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);

        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Any(a => a.Key == "ShouldReturnUnauthorized"))
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);

        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }
}
