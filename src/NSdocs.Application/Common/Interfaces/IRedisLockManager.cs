using System;
using System.Threading.Tasks;

namespace NSdocs.Application.Common.Interfaces;

/// <summary>
/// Provides distributed locking capabilities using Redis.
/// </summary>
public interface IRedisLockManager
{
    /// <summary>
    /// Attempts to acquire a lock for a specific resource.
    /// </summary>
    /// <param name="resourceKey">The unique key identifying the resource to lock.</param>
    /// <param name="lockIdentifier">A unique identifier for the lock holder (e.g., instance ID).</param>
    /// <param name="expiry">The time-to-live for the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the lock was acquired successfully, false otherwise.</returns>
    Task<bool> AcquireLockAsync(string resourceKey, string lockIdentifier, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a lock held by the specified identifier.
    /// </summary>
    /// <param name="resourceKey">The unique key identifying the resource whose lock is to be released.</param>
    /// <param name="lockIdentifier">The unique identifier of the lock holder attempting to release the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the lock was successfully released, false otherwise (e.g., lock didn't exist or identifier didn't match).</returns>
    Task<bool> ReleaseLockAsync(string resourceKey, string lockIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extends the expiry time of an existing lock, if held by the specified identifier.
    /// </summary>
    /// <param name="resourceKey">The unique key identifying the resource whose lock expiry is to be extended.</param>
    /// <param name="lockIdentifier">The unique identifier of the lock holder.</param>
    /// <param name="newExpiry">The new time-to-live for the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the lock expiry was successfully extended, false otherwise.</returns>
    Task<bool> ExtendLockAsync(string resourceKey, string lockIdentifier, TimeSpan newExpiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a lock is currently held for the specified resource.
    /// </summary>
    /// <param name="resourceKey">The unique key identifying the resource.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the lock is held, false otherwise.</returns>
    Task<bool> IsLockHeldAsync(string resourceKey, CancellationToken cancellationToken = default);
}
