using Microsoft.Extensions.Logging;

namespace AuthorizationInterceptor.Log;

public static partial class AuthorizationInterceptorLogDifinitions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "No interceptor was configured for HttpClient `{httpClientName}`. A Runtime interceptor was used instead. It is recommended to use at least the MemoryCache interceptor.")]
    public static partial void LogNoInterceptorUsed(this ILogger logger, string httpClientName);
}

