using System.Collections.Concurrent;

namespace AuthorizationInterceptor.Utils;

/// <summary>
/// Provides asynchronous mutual exclusion scoped to a string key, so that only one caller at a time
/// authenticates for a given HttpClient name and cache key suffix.
/// Entries are reference counted and dropped as soon as nobody holds or awaits them, which keeps
/// memory bounded even when <c>CacheKeyBuilder</c> produces one key per user or tenant.
/// </summary>
internal sealed class KeyedAsyncLock
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    /// <summary>
    /// Acquires the lock for <paramref name="key"/>.
    /// </summary>
    /// <returns>
    /// A handle that must be disposed to release the lock. <see cref="Handle.WasContended"/> reports
    /// whether the caller had to wait for another holder, which means the shared state it is guarding
    /// may have changed in the meantime.
    /// </returns>
    public async ValueTask<Handle> AcquireAsync(string key, CancellationToken cancellationToken)
    {
        var entry = Rent(key);
        var contended = false;

        try
        {
            if (!entry.Semaphore.Wait(0, CancellationToken.None))
            {
                contended = true;
                await entry.Semaphore.WaitAsync(cancellationToken);
            }
        }
        catch
        {
            Return(key, entry);
            throw;
        }

        return new Handle(this, key, entry, contended);
    }

    private Entry Rent(string key)
    {
        while (true)
        {
            var entry = _entries.GetOrAdd(key, static _ => new Entry());

            lock (entry.SyncRoot)
            {
                if (!entry.Retired)
                {
                    entry.References++;
                    return entry;
                }
            }

            // The entry was retired between the lookup and the lock; take a fresh one.
        }
    }

    private void Return(string key, Entry entry)
    {
        lock (entry.SyncRoot)
        {
            if (--entry.References > 0)
                return;

            entry.Retired = true;
        }

        _entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
        entry.Semaphore.Dispose();
    }

    internal sealed class Handle(KeyedAsyncLock owner, string key, Entry entry, bool wasContended) : IDisposable
    {
        private bool _released;

        /// <summary>
        /// Whether another caller already held this lock when the acquisition started.
        /// </summary>
        public bool WasContended => wasContended;

        public void Dispose()
        {
            if (_released)
                return;

            _released = true;
            entry.Semaphore.Release();
            owner.Return(key, entry);
        }
    }

    internal sealed class Entry
    {
        public readonly object SyncRoot = new();

        public readonly SemaphoreSlim Semaphore = new(1, 1);

        public int References;

        public bool Retired;
    }
}
