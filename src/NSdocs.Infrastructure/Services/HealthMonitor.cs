using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;
using StackExchange.Redis;

namespace NSdocs.Infrastructure.Services;

// TODO: Consider making this an IHostedService for automatic start/stop
public class HealthMonitor : IHealthMonitor
{
    private readonly IDatabase _redis;
    private readonly IWorkDistributor _workDistributor;
    private readonly ILogger<HealthMonitor> _logger;
    private readonly string _instanceId;
    private readonly TimeSpan _heartbeatInterval;
    private readonly TimeSpan _instanceExpiry; // Should match WorkDistributor's expiry
    private readonly string _activeFlushersKey = "flushers:active"; // Sorted set: member=instanceId, score=timestamp

    // TODO: Instance ID, HeartbeatInterval, InstanceExpiry should come from configuration
    public HealthMonitor(
        IRedisConnectionFactory redisFactory,
        IWorkDistributor workDistributor,
        ILogger<HealthMonitor> logger,
        string instanceId = "default-instance", // Temporary default
        TimeSpan? heartbeatInterval = null,
        TimeSpan? instanceExpiry = null)
    {
        _redis = redisFactory.GetDatabase();
        _workDistributor = workDistributor;
        _logger = logger;
        _instanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
        _heartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(30);
        _instanceExpiry = instanceExpiry ?? TimeSpan.FromMinutes(5); // Default expiry
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health monitor started for instance {InstanceId}. Heartbeat interval: {HeartbeatInterval}, Instance expiry: {InstanceExpiry}", 
            _instanceId, _heartbeatInterval, _instanceExpiry);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 1. Send Heartbeat
                await SendHeartbeatAsync(cancellationToken);

                // 2. Check for expired instances and trigger redistribution if needed
                await CheckAndHandleExpiredInstancesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Health monitoring cancellation requested for instance {InstanceId}.", _instanceId);
                break; // Exit loop if cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health monitoring cycle for instance {InstanceId}.", _instanceId);
                // Avoid tight loop on continuous errors
            }
            
            // Wait for the next interval, respecting cancellation
            try
            {
                await Task.Delay(_heartbeatInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                 _logger.LogInformation("Health monitoring delay cancelled for instance {InstanceId}.", _instanceId);
                 break; // Exit loop if cancellation is requested during delay
            }
        }
        
        _logger.LogInformation("Health monitor stopped for instance {InstanceId}.", _instanceId);
    }

    public async Task<ISet<string>> GetActiveInstancesAsync(CancellationToken cancellationToken = default)
    {
        await CleanupExpiredInstancesAsync(cancellationToken); // Ensure list is fresh
        var activeInstances = await _redis.SortedSetRangeByRankAsync(_activeFlushersKey);
        return new HashSet<string>(activeInstances.Select(m => m.ToString()));
    }

    public async Task<bool> IsInstanceActiveAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var score = await _redis.SortedSetScoreAsync(_activeFlushersKey, instanceId);
        if (!score.HasValue) return false; // Not in the set

        var cutoffScore = DateTimeOffset.UtcNow.Subtract(_instanceExpiry).ToUnixTimeMilliseconds();
        return score.Value > cutoffScore; // Check if score is within expiry window
    }

    private async Task SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        var score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _redis.SortedSetAddAsync(_activeFlushersKey, _instanceId, score, flags: CommandFlags.FireAndForget);
        _logger.LogDebug("Instance {InstanceId} sent heartbeat with score {Score}.", _instanceId, score);
    }

    private async Task CheckAndHandleExpiredInstancesAsync(CancellationToken cancellationToken)
    {
        var removedCount = await CleanupExpiredInstancesAsync(cancellationToken);
        if (removedCount > 0)
        {
            _logger.LogInformation("Detected {Count} expired instances. Triggering work redistribution.", removedCount);
            // Trigger redistribution non-blockingly
            _ = _workDistributor.TriggerRedistributionAsync(cancellationToken); 
        }
    }
    
    private async Task<long> CleanupExpiredInstancesAsync(CancellationToken cancellationToken = default)
    {
        // Remove instances whose last heartbeat (score) is older than the expiry time
        var cutoffScore = DateTimeOffset.UtcNow.Subtract(_instanceExpiry).ToUnixTimeMilliseconds();
        // Remove scores from -inf up to the cutoff time
        var removedCount = await _redis.SortedSetRemoveRangeByScoreAsync(_activeFlushersKey, double.NegativeInfinity, cutoffScore); 
        if (removedCount > 0)
        {
            _logger.LogDebug("Removed {Count} expired instances with score <= {CutoffScore}.", removedCount, cutoffScore);
        }
        return removedCount;
    }
}
