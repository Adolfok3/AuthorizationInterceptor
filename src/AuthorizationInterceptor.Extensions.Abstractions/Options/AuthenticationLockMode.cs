namespace AuthorizationInterceptor.Extensions.Abstractions.Options;

/// <summary>
/// Defines how concurrent authentication attempts against the target API are serialized.
/// The modes are independent and can be combined (e.g. <see cref="Local"/> | <see cref="Distributed"/>).
/// </summary>
[Flags]
public enum AuthenticationLockMode
{
    /// <summary>
    /// No lock. Concurrent callers may authenticate against the target API in parallel.
    /// </summary>
    None = 0,

    /// <summary>
    /// Local lock (single-flight). Prevents concurrent authentication within a single instance,
    /// so simultaneous callers on the same process share a single authentication call.
    /// </summary>
    Local = 1,

    /// <summary>
    /// Distributed lock. Prevents concurrent authentication across multiple instances, so only one
    /// instance authenticates against the target API at a time.
    /// </summary>
    Distributed = 2
}
