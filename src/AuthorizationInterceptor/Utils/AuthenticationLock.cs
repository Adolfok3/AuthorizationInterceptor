using AuthorizationInterceptor.Extensions.Abstractions.Headers;
using AuthorizationInterceptor.Extensions.Abstractions.Options;
using Medallion.Threading;

namespace AuthorizationInterceptor.Utils;

/// <summary>
/// Coordinates how concurrent authentication attempts are serialized according to the configured
/// <see cref="AuthenticationLockMode"/>: a local single-flight lock (concurrency within the instance)
/// and/or a distributed lock (concurrency across multiple instances).
/// </summary>
internal sealed class AuthenticationLock
{
    private const string LockNamePrefix = "authorizationinterceptor:";

    private readonly AuthenticationLockMode _mode;
    private readonly AuthenticationSingleFlight _singleFlight;
    private readonly IDistributedLockProvider? _distributedLockProvider;
    private readonly TimeSpan? _distributedLockTimeout;

    public AuthenticationLock(AuthenticationLockMode mode, AuthenticationSingleFlight singleFlight, IDistributedLockProvider? distributedLockProvider, TimeSpan? distributedLockTimeout)
    {
        if (mode.HasFlag(AuthenticationLockMode.Distributed) && distributedLockProvider == null)
            throw new InvalidOperationException($"{nameof(AuthenticationLockMode)}.{nameof(AuthenticationLockMode.Distributed)} requires an {nameof(IDistributedLockProvider)} (from DistributedLock.Core) to be registered in the service collection. Register a concrete provider such as Redis, SqlServer, Postgres, Azure, or FileSystem as a singleton.");

        _mode = mode;
        _singleFlight = singleFlight;
        _distributedLockProvider = distributedLockProvider;
        _distributedLockTimeout = distributedLockTimeout;
    }

    /// <summary>
    /// Runs <paramref name="authenticate"/> under the configured locks. When the distributed lock is enabled,
    /// <paramref name="revalidate"/> is invoked after the lock is acquired so a caller can adopt headers that a
    /// concurrent instance may have already refreshed (double-checked locking), skipping the authentication.
    /// </summary>
    public async ValueTask<AuthenticationLockResult> RunAsync(
        string key,
        Func<CancellationToken, Task<AuthorizationHeaders?>> authenticate,
        Func<CancellationToken, Task<AuthorizationHeaders?>> revalidate,
        CancellationToken cancellationToken)
    {
        if (!_mode.HasFlag(AuthenticationLockMode.Local))
        {
            var headers = await AcquireDistributedAndRunAsync(key, authenticate, revalidate, cancellationToken);
            return new AuthenticationLockResult(headers, false);
        }

        var flight = await _singleFlight.RunAsync(
            key,
            () => AcquireDistributedAndRunAsync(key, authenticate, revalidate, CancellationToken.None),
            cancellationToken);

        return new AuthenticationLockResult(flight.Headers, flight.Joined);
    }

    private async Task<AuthorizationHeaders?> AcquireDistributedAndRunAsync(
        string key,
        Func<CancellationToken, Task<AuthorizationHeaders?>> authenticate,
        Func<CancellationToken, Task<AuthorizationHeaders?>> revalidate,
        CancellationToken cancellationToken)
    {
        if (!_mode.HasFlag(AuthenticationLockMode.Distributed))
            return await authenticate(cancellationToken);

        await using var handle = await _distributedLockProvider!.AcquireLockAsync(LockNamePrefix + key, _distributedLockTimeout, cancellationToken);

        return await revalidate(cancellationToken) ?? await authenticate(cancellationToken);
    }

    internal readonly record struct AuthenticationLockResult(AuthorizationHeaders? Headers, bool Joined);
}
