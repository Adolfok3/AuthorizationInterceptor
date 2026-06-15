using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace AuthorizationInterceptor.Utils;

[ExcludeFromCodeCoverage]
public static partial class AuthorizationInterceptorLogDefinitions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "No interceptor was configured for HttpClient `{httpClientName}`. A Runtime interceptor was used instead. It is recommended to use at least the MemoryCache interceptor.")]
    public static partial void LogNoInterceptorUsed(this ILogger logger, string httpClientName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "AuthorizationInterceptor is not available for synchronous requests. Consider using asynchronous requests!")]
    public static partial void LogUnavailableForSyncRequests(this ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Operation canceled while getting headers from interceptor `{interceptor}` with integration `{httpClientName}`")]
    public static partial void LogOperationCanceledInInterceptor(this ILogger logger, string interceptor, string httpClientName);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "No headers added to request with integration `{Name}` and cache key suffix `{CacheKeySuffix}`")]
    public static partial void LogNoHeadersAddedToRequest(this ILogger logger, string name, string? cacheKeySuffix);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Caught unauthenticated predicate from response with integration `{Name}` and cache key suffix `{CacheKeySuffix}`")]
    public static partial void CaughtUnauthenticatedPredicateFromResponse(this ILogger logger, string name, string? cacheKeySuffix);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Adding header `{Header}` to request with integration `{Name}` and cache key suffix `{CacheKeySuffix}`")]
    public static partial void LogAddingHeader(this ILogger logger, string header, string name, string? cacheKeySuffix);
}

