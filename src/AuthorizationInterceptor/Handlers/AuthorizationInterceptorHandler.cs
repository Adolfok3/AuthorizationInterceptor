﻿using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Strategies;
using AuthorizationInterceptor.Utils;
using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Handlers;

internal class AuthorizationInterceptorHandler : DelegatingHandler
{
    private readonly string _name;
    private readonly Func<HttpResponseMessage, bool> _unauthenticatedPredicate;
    private readonly IAuthenticationHandler _authenticationHandler;
    private readonly IAuthorizationInterceptorStrategy _strategy;
    private readonly ILogger _logger;

    public AuthorizationInterceptorHandler(string name, Func<HttpResponseMessage, bool> unauthenticatedPredicate, IAuthenticationHandler authenticationHandler, IAuthorizationInterceptorStrategy strategy, ILoggerFactory loggerFactory)
    {
        _name = name;
        _strategy = strategy;
        _authenticationHandler = authenticationHandler;
        _unauthenticatedPredicate = unauthenticatedPredicate;
        _logger = loggerFactory.CreateLogger("AuthorizationInterceptorHandler");
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
        var headers = await _strategy.GetHeadersAsync(_name, _authenticationHandler, cancellationToken);
        if (headers == null || !headers.Any())
        {
            LogDebug("No headers added to request with integration '{name}'", _name);
            return await base.SendAsync(request, cancellationToken);
        }

        request = AddHeaders(request, headers);

        var response = await base.SendAsync(request, cancellationToken);
        if (!_unauthenticatedPredicate(response))
            return response;

        LogDebug("Caught unauthenticated predicate from response with integration '{name}'", _name);
        headers = await _strategy.UpdateHeadersAsync(_name, headers, _authenticationHandler, cancellationToken);
        if (headers == null || !headers.Any())
        {
            LogDebug("No headers added to request with integration '{name}'", _name);
            return response;
        }

        request = AddHeaders(request, headers);

        return await base.SendAsync(request, cancellationToken);
    }

    private HttpRequestMessage AddHeaders(HttpRequestMessage request, AuthorizationHeaders headers)
    {
        foreach (var header in headers)
        {
            LogDebug("Adding header '{header}' to request with integration '{name}'", header.Key, _name);
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return request;
    }

    private void LogDebug(string message, params object[] objs)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug(message, objs);
    }
}
