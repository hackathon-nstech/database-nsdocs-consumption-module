using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NSdocs.Application.Common.Interfaces;

/// <summary>
/// Monitors the health of worker instances and detects failures.
/// </summary>
public interface IHealthMonitor
{
    /// <summary>
    /// Starts the background monitoring process (e.g., publishing heartbeats, checking for expired instances).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop monitoring.</param>
    /// <returns>A task representing the asynchronous monitoring loop.</returns>
    Task StartMonitoringAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the set of currently active and healthy instance IDs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A set of active instance IDs.</returns>
    Task<ISet<string>> GetActiveInstancesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific instance is considered active and healthy.
    /// </summary>
    /// <param name="instanceId">The ID of the instance to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the instance is active, false otherwise.</returns>
    Task<bool> IsInstanceActiveAsync(string instanceId, CancellationToken cancellationToken = default);
}
