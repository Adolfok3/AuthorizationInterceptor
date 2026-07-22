using AuthorizationInterceptor.Extensions.Abstractions.Handlers;
using AuthorizationInterceptor.Extensions.Abstractions.Headers;

namespace AuthorizationInterceptor.Tests.Utils;

/// <summary>
/// Authentication handler that counts how many times it was invoked and takes a configurable amount of
/// time, so concurrent callers reliably overlap.
/// </summary>
public class MockCountingAuthenticationHandler(TimeSpan delay) : IAuthenticationHandler
{
    private int _calls;

    public int Calls => Volatile.Read(ref _calls);

    public async ValueTask<AuthorizationHeaders?> AuthenticateAsync(AuthorizationHeaders? expiredHeaders, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        await Task.Delay(delay, cancellationToken);

        return MockAuthorizationHeaders.CreateHeaders();
    }
}
