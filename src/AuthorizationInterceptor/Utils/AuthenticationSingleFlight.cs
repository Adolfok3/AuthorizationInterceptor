using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using System.Collections.Concurrent;

namespace AuthorizationInterceptor.Utils;

/// <summary>
/// Coalesces concurrent authentications that share a key: the first caller runs the operation and
/// every caller arriving while it is still in flight awaits that same result instead of
/// authenticating again.
/// An entry exists only while its operation is running, so per-user or per-tenant cache keys do not
/// accumulate.
/// </summary>
internal sealed class AuthenticationSingleFlight
{
    // Ordinal on purpose: the cache backends compare keys case-sensitively, so anything looser here
    // would let two callers share one authentication while reading and writing separate cache entries.
    private readonly ConcurrentDictionary<string, Lazy<Task<AuthorizationHeaders?>>> _inFlight = new(StringComparer.Ordinal);

    /// <param name="operation">
    /// Runs detached from any single caller's cancellation, so a caller that gives up does not abort
    /// the authentication the remaining ones are waiting for.
    /// </param>
    /// <param name="cancellationToken">Applies to this caller's wait only, never to the shared operation.</param>
    public async ValueTask<FlightResult> RunAsync(string key, Func<Task<AuthorizationHeaders?>> operation, CancellationToken cancellationToken)
    {
        // Created eagerly so the reference can be compared against whatever the dictionary hands back,
        // which is what tells this caller whether it started the flight or joined one.
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
            // While this flight is registered, GetOrAdd can only hand it out and never replace it, so
            // removing by key alone cannot drop somebody else's flight.
            _inFlight.TryRemove(key, out _);
        }
    }

    /// <param name="Joined">True when another caller had already started this authentication.</param>
    internal readonly record struct FlightResult(AuthorizationHeaders? Headers, bool Joined);
}
