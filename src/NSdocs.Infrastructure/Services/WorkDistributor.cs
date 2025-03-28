using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;
using StackExchange.Redis;

namespace NSdocs.Infrastructure.Services;

public class WorkDistributor : IWorkDistributor
{
    private readonly IDatabase _redis;
    private readonly IRedisLockManager _lockManager;
    private readonly ILogger<WorkDistributor> _logger;
    private readonly string _instanceId; // Unique ID for this worker instance
    private readonly string _activeFlushersKey = "flushers:active"; // Sorted set: member=instanceId, score=timestamp
    private readonly string _allCompaniesKey = "agg:companies"; // Set: member=companyId
    private readonly string _redistributionLockKey = "lock:workdistributor:redistribute";
    private readonly TimeSpan _redistributionLockExpiry = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _instanceExpiry = TimeSpan.FromMinutes(5); // How long an instance stays in active set without update

    private ISet<int>? _assignedWorkCache; // Cache for assigned work

    // TODO: Instance ID should likely come from configuration or environment
    public WorkDistributor(
        IRedisConnectionFactory redisFactory,
        IRedisLockManager lockManager,
        ILogger<WorkDistributor> logger,
        string instanceId = "default-instance") // Temporary default
    {
        _redis = redisFactory.GetDatabase();
        _lockManager = lockManager;
        _logger = logger;
        _instanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
    }

    public async Task<ISet<int>> GetAssignedWorkAsync(CancellationToken cancellationToken = default)
    {
        if (_assignedWorkCache != null)
        {
            return _assignedWorkCache;
        }

        _logger.LogInformation("Instance {InstanceId} attempting to get assigned work.", _instanceId);
        await RegisterOrUpdateInstanceAsync(cancellationToken); // Ensure instance is registered/updated

        var assignedWork = await CalculateWorkAssignmentAsync(cancellationToken);
        _assignedWorkCache = assignedWork.GetValueOrDefault(_instanceId, new HashSet<int>());

        _logger.LogInformation("Instance {InstanceId} assigned {WorkCount} companies.", _instanceId, _assignedWorkCache.Count);
        return _assignedWorkCache;
    }

    public async Task ReleaseWorkAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Instance {InstanceId} releasing work.", _instanceId);
        _assignedWorkCache = null; // Clear cache
        await _redis.SortedSetRemoveAsync(_activeFlushersKey, _instanceId);
        _logger.LogInformation("Instance {InstanceId} removed from active set.", _instanceId);
        // Optionally trigger redistribution if needed, but usually handled by health monitor or startup
    }

    public async Task TriggerRedistributionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Instance {InstanceId} triggering work redistribution.", _instanceId);
        
        // Attempt to acquire global redistribution lock
        var lockAcquired = await _lockManager.AcquireLockAsync(_redistributionLockKey, _instanceId, _redistributionLockExpiry, cancellationToken);
        if (!lockAcquired)
        {
            _logger.LogDebug("Instance {InstanceId} could not acquire redistribution lock, another instance is likely handling it.", _instanceId);
            return; // Another instance is handling redistribution
        }

        try
        {
            _logger.LogInformation("Instance {InstanceId} acquired redistribution lock. Recalculating assignments...", _instanceId);
            // Actual redistribution logic happens implicitly when GetAssignedWorkAsync is called by instances
            // This trigger mainly ensures cleanup and potentially notifies instances if using pub/sub
            await CleanupExpiredInstancesAsync(cancellationToken);
            _assignedWorkCache = null; // Clear local cache to force recalculation on next GetAssignedWorkAsync
            _logger.LogInformation("Instance {InstanceId} completed redistribution trigger actions.", _instanceId);
        }
        finally
        {
            await _lockManager.ReleaseLockAsync(_redistributionLockKey, _instanceId, cancellationToken);
            _logger.LogDebug("Instance {InstanceId} released redistribution lock.", _instanceId);
        }
    }
    
    public async Task<long> GetActiveInstanceCountAsync(CancellationToken cancellationToken = default)
    {
        await CleanupExpiredInstancesAsync(cancellationToken);
        return await _redis.SortedSetLengthAsync(_activeFlushersKey);
    }

    public async Task<ISet<int>> GetAllWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        var companyIds = await _redis.SetMembersAsync(_allCompaniesKey);
        return new HashSet<int>(companyIds.Select(id => (int)id).Where(id => id > 0)); // Filter potentially invalid entries
    }

    private async Task RegisterOrUpdateInstanceAsync(CancellationToken cancellationToken = default)
    {
        var score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redis.SortedSetAddAsync(_activeFlushersKey, _instanceId, score);
        _logger.LogDebug("Instance {InstanceId} registered/updated in active set with score {Score}.", _instanceId, score);
    }

    private async Task CleanupExpiredInstancesAsync(CancellationToken cancellationToken = default)
    {
        var cutoffScore = DateTimeOffset.UtcNow.Subtract(_instanceExpiry).ToUnixTimeMilliseconds();
        var removedCount = await _redis.SortedSetRemoveRangeByScoreAsync(_activeFlushersKey, -1, cutoffScore);
        if (removedCount > 0)
        {
            _logger.LogInformation("Removed {Count} expired instances from active set.", removedCount);
            // Expired instances found, clear local cache as assignments might change
             _assignedWorkCache = null; 
        }
    }

    private async Task<Dictionary<string, HashSet<int>>> CalculateWorkAssignmentAsync(CancellationToken cancellationToken = default)
    {
        await CleanupExpiredInstancesAsync(cancellationToken); // Ensure list is up-to-date

        var activeInstances = (await _redis.SortedSetRangeByRankAsync(_activeFlushersKey)).Select(m => m.ToString()).ToList();
        var allCompanies = await GetAllWorkItemsAsync(cancellationToken);

        var assignments = new Dictionary<string, HashSet<int>>();
        if (!activeInstances.Any() || !allCompanies.Any())
        {
            _logger.LogWarning("No active instances or no companies to assign. Returning empty assignments.");
            return assignments; // No instances or no work
        }

        // Initialize assignment dictionary
        foreach (var instance in activeInstances)
        {
            assignments[instance] = new HashSet<int>();
        }

        // Assign companies using consistent hashing (modulo based on instance index)
        int instanceCount = activeInstances.Count;
        foreach (var companyId in allCompanies)
        {
            // Simple modulo hashing based on company ID and instance count
            // Ensure non-negative index
            int instanceIndex = (int)(((uint)companyId.GetHashCode()) % instanceCount); 
            var assignedInstance = activeInstances[instanceIndex];
            assignments[assignedInstance].Add(companyId);
        }

        _logger.LogDebug("Calculated work assignments for {InstanceCount} instances and {CompanyCount} companies.", instanceCount, allCompanies.Count);
        foreach(var kvp in assignments)
        {
            _logger.LogTrace("Instance {InstanceId} assigned {Count} companies.", kvp.Key, kvp.Value.Count);
        }

        return assignments;
    }
}
