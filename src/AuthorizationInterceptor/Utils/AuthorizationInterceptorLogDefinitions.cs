using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace AuthorizationInterceptor.Utils;

[ExcludeFromCodeCoverage]
public static partial class AuthorizationInterceptorLogDefinitions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "No interceptor was configured for integration `{integrationKey}`. A Runtime interceptor was used instead. It is recommended to use at least the MemoryCache interceptor.")]
    public static partial void LogNoInterceptorUsed(this ILogger logger, string integrationKey);

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

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Headers were refreshed by a concurrent caller with integration `{IntegrationKey}`. Skipping authentication.")]
    public static partial void LogHeadersRefreshedByConcurrentCaller(this ILogger logger, string integrationKey);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Getting headers from interceptor `{Interceptor}` with integration `{IntegrationKey}`")]
    public static partial void LogGettingHeadersFromInterceptor(this ILogger logger, string interceptor, string integrationKey);

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "Headers found in interceptor `{Interceptor}` with integration `{IntegrationKey}`")]
    public static partial void LogHeadersFoundInInterceptor(this ILogger logger, string interceptor, string integrationKey);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Headers still valid in interceptor `{Interceptor}` with integration `{IntegrationKey}`")]
    public static partial void LogHeadersStillValidInInterceptor(this ILogger logger, string interceptor, string integrationKey);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Headers are expired in interceptor `{Interceptor}` with integration `{IntegrationKey}`")]
    public static partial void LogHeadersExpiredInInterceptor(this ILogger logger, string interceptor, string integrationKey);

    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Error getting headers from interceptor `{Interceptor}` with integration `{IntegrationKey}`")]
    public static partial void LogErrorGettingHeadersFromInterceptor(this ILogger logger, Exception exception, string interceptor, string integrationKey);

    [LoggerMessage(EventId = 13, Level = LogLevel.Debug, Message = "Updating headers in interceptor `{Interceptor}` with integration `{IntegrationKey}`")]
    public static partial void LogUpdatingHeadersInInterceptor(this ILogger logger, string interceptor, string integrationKey);

    [LoggerMessage(EventId = 14, Level = LogLevel.Error, Message = "Error updating headers in interceptor `{Interceptor}` with integration `{IntegrationKey}`")]
    public static partial void LogErrorUpdatingHeadersInInterceptor(this ILogger logger, Exception exception, string interceptor, string integrationKey);

    [LoggerMessage(EventId = 15, Level = LogLevel.Debug, Message = "Getting new headers from AuthenticationHandler `{AuthenticationHandler}` with integration `{IntegrationKey}`")]
    public static partial void LogGettingNewHeadersFromAuthenticationHandler(this ILogger logger, string authenticationHandler, string integrationKey);

    [LoggerMessage(EventId = 16, Level = LogLevel.Debug, Message = "No new headers generated in AuthenticationHandler `{AuthenticationHandler}` with integration `{IntegrationKey}`")]
    public static partial void LogNoNewHeadersGenerated(this ILogger logger, string authenticationHandler, string integrationKey);
}

