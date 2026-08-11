using Medallion.Threading;

namespace AuthorizationInterceptor.Tests.Utils;

/// <summary>
/// Builds a substituted <see cref="IDistributedLockProvider"/> chain (provider -> lock -> handle) so the
/// distributed-lock path can be exercised without a real provider. An optional <c>onAcquire</c> callback
/// runs when the lock is acquired, letting a test simulate another instance refreshing the shared cache
/// while this caller was waiting for the lock.
/// </summary>
public sealed class MockDistributedLock
{
    public IDistributedLockProvider Provider { get; }

    public IDistributedLock Lock { get; }

    public IDistributedSynchronizationHandle Handle { get; }

    public string? LastLockName { get; private set; }

    private int _acquireCount;

    public int AcquireCount => Volatile.Read(ref _acquireCount);

    public MockDistributedLock(Action? onAcquire = null)
    {
        Handle = Substitute.For<IDistributedSynchronizationHandle>();
        Lock = Substitute.For<IDistributedLock>();
        Provider = Substitute.For<IDistributedLockProvider>();

        Provider.CreateLock(Arg.Any<string>()).Returns(callInfo =>
        {
            LastLockName = callInfo.Arg<string>();
            return Lock;
        });

        Lock.AcquireAsync(Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Interlocked.Increment(ref _acquireCount);
            onAcquire?.Invoke();
            return new ValueTask<IDistributedSynchronizationHandle>(Handle);
        });
    }
}
