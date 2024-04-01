using AuthorizationInterceptor.Entries;
using AuthorizationInterceptor.Interceptors;
using AuthorizationInterceptor.Options;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AuthorizationInterceptor.Handlers
{
    internal class AuthorizationInterceptorHandler : DelegatingHandler
    {
        private readonly IAuthorizationInterceptor _interceptor;
        private readonly AuthorizationInterceptorOptions _options;
        private readonly ILogger _logger;

        public AuthorizationInterceptorHandler(IAuthorizationInterceptor interceptor, AuthorizationInterceptorOptions options, ILogger<AuthorizationInterceptorHandler> logger)
        {
            _interceptor = interceptor;
            _options = options;
            _logger = logger;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendWithInterceptorAsync(request, cancellationToken).GetAwaiter().GetResult();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await SendWithInterceptorAsync(request, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendWithInterceptorAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = await _interceptor.GetHeadersAsync();
            request = AddHeaders(request, headers);

            var response = await base.SendAsync(request, cancellationToken);
            if (!_options.UnauthenticatedPredicate(response))
                return response;

            Log("Caught unauthenticated predicate from response");
            headers = await _interceptor.UpdateHeadersAsync(headers);
            request = AddHeaders(request, headers);

            return await base.SendAsync(request, cancellationToken);
        }

        private HttpRequestMessage AddHeaders(HttpRequestMessage request, AuthorizationEntry headers)
        {
            if (headers == null || !headers.Any())
            {
                Log("No headers added to request");
                return request;
            }

            foreach (var header in headers)
            {
                Log("Adding header '{header}' to request", header.Key);
                request.Headers.Remove(header.Key);
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return request;
        }

        private void Log(string message, params object[] objs)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(message, objs);
        }
    }
}
