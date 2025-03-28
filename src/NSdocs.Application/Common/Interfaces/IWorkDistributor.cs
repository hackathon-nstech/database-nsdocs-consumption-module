using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NSdocs.Application.Common.Interfaces;

/// <summary>
/// Manages the distribution of work (e.g., company IDs to process) among multiple worker instances.
/// </summary>
public interface IWorkDistributor
{
    /// <summary>
    /// Gets the set of company IDs currently assigned to this worker instance.
    /// This might involve claiming work if not already assigned.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A set of company IDs assigned to the current instance.</returns>
    Task<ISet<int>> GetAssignedWorkAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals that the current instance is releasing its assigned work.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReleaseWorkAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a recalculation and potential redistribution of work across all active instances.
    /// Typically triggered when instances join or leave.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TriggerRedistributionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of active worker instances participating in work distribution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of active instances.</returns>
    Task<long> GetActiveInstanceCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all company IDs that need processing across all instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A set of all company IDs requiring work.</returns>
    Task<ISet<int>> GetAllWorkItemsAsync(CancellationToken cancellationToken = default);
}
