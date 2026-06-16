using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Strategies;
using AuthorizationInterceptor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Handlers;

internal class AuthorizationInterceptorHandler : DelegatingHandler
{
    private readonly string _name;
    private readonly Func<HttpResponseMessage, bool> _unauthenticatedPredicate;
    private readonly IAuthenticationHandler _authenticationHandler;
    private readonly IAuthorizationInterceptorStrategy _strategy;
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Func<IHttpContextAccessor, string?>? _cacheKeyBuilder;

    public AuthorizationInterceptorHandler(string name, Func<HttpResponseMessage, bool> unauthenticatedPredicate, IAuthenticationHandler authenticationHandler, IAuthorizationInterceptorStrategy strategy, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor, Func<IHttpContextAccessor, string?>? cacheKeyBuilder = null)
    {
        _name = name;
        _strategy = strategy;
        _authenticationHandler = authenticationHandler;
        _unauthenticatedPredicate = unauthenticatedPredicate;
        _logger = loggerFactory.CreateLogger("AuthorizationInterceptorHandler");
        _httpContextAccessor = httpContextAccessor;
        _cacheKeyBuilder = cacheKeyBuilder;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogUnavailableForSyncRequests();
        return base.Send(request, cancellationToken);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => await SendWithInterceptorAsync(request, cancellationToken);

    private async Task<HttpResponseMessage> SendWithInterceptorAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cacheKeySuffix = GetCacheKeySuffix();

        var headers = await _strategy.GetHeadersAsync(_name, _authenticationHandler, cacheKeySuffix, cancellationToken);
        if (headers == null || !headers.Any())
        {
            _logger.LogNoHeadersAddedToRequest(_name, cacheKeySuffix);
            return await base.SendAsync(request, cancellationToken);
        }

        request = AddHeaders(request, headers, cacheKeySuffix);

        var response = await base.SendAsync(request, cancellationToken);
        if (!_unauthenticatedPredicate(response))
            return response;

        _logger.CaughtUnauthenticatedPredicateFromResponse(_name, cacheKeySuffix);

        headers = await _strategy.UpdateHeadersAsync(_name, headers, _authenticationHandler, cacheKeySuffix, cancellationToken);
        if (headers == null || !headers.Any())
        {
            _logger.LogNoHeadersAddedToRequest(_name, cacheKeySuffix);
            return response;
        }

        request = AddHeaders(request, headers, cacheKeySuffix);

        return await base.SendAsync(request, cancellationToken);
    }

    private string? GetCacheKeySuffix()
    {
        if (_cacheKeyBuilder is null)
            return null;

        return _cacheKeyBuilder.Invoke(_httpContextAccessor);
    }

    private HttpRequestMessage AddHeaders(HttpRequestMessage request, AuthorizationHeaders headers, string? cacheKeySuffix)
    {
        foreach (var header in headers)
        {
            _logger.LogAddingHeader(header.Key, _name, cacheKeySuffix);
            request.Headers.Remove(header.Key);
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return request;
    }
}
