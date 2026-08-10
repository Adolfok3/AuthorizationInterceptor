using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using System.Collections.Concurrent;

namespace AuthorizationInterceptor.Utils;

internal sealed class AuthenticationSingleFlight
{
    private readonly ConcurrentDictionary<string, Lazy<Task<AuthorizationHeaders?>>> _inFlight = new(StringComparer.Ordinal);

    public async ValueTask<FlightResult> RunAsync(string key, Func<Task<AuthorizationHeaders?>> operation, CancellationToken cancellationToken)
    {
        var created = new Lazy<Task<AuthorizationHeaders?>>(() => RunAndEvictAsync(key, operation));
        var flight = _inFlight.GetOrAdd(key, created);

        var headers = await flight.Value.WaitAsync(cancellationToken);

        return new FlightResult(headers, !ReferenceEquals(flight, created));
    }

    private async Task<AuthorizationHeaders?> RunAndEvictAsync(string key, Func<Task<AuthorizationHeaders?>> operation)
    {
        try
        {
            return await operation();
        }
        finally
        {
            _inFlight.TryRemove(key, out _);
        }
    }

    internal readonly record struct FlightResult(AuthorizationHeaders? Headers, bool Joined);
}
