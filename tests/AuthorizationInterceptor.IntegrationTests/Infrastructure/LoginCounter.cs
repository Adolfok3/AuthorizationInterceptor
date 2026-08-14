namespace AuthorizationInterceptor.IntegrationTests.Infrastructure;

/// <summary>
/// Thread-safe counter of how many times the target API's authentication endpoint was actually hit.
/// It is the oracle for every lock test: the guarantee "only one login happened" reduces to Count == 1.
/// </summary>
public sealed class LoginCounter
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Increment() => Interlocked.Increment(ref _count);
}
