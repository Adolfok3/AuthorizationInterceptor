namespace AuthorizationInterceptor.Tests.Utils;

internal class MockAuthorizationInterceptorHandler : DelegatingHandler
{
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
