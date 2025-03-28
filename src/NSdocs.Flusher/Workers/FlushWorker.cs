using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Added for InstanceId
using Microsoft.Extensions.DependencyInjection; // Added for IServiceScopeFactory usage
using Microsoft.Extensions.Hosting; // Added for BackgroundService
using Microsoft.Extensions.Logging; // Added for ILogger
using Microsoft.Extensions.Options;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Infrastructure.Configuration;
using NSdocs.Infrastructure.Services; // Assuming Redis services are here
using StackExchange.Redis;

namespace NSdocs.Flusher.Workers;

public class FlushWorker : BackgroundService
{
    private readonly ILogger<FlushWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRedisConnectionFactory _redisFactory; // Keep for direct delta access
    private readonly IWorkDistributor _workDistributor;
    private readonly IRedisLockManager _lockManager;
    private readonly string _instanceId;
    private readonly RedisSettings _redisSettings;
    private readonly TimeSpan _companyLockExpiry = TimeSpan.FromMinutes(1); // How long to lock a company for processing

    public FlushWorker(
        ILogger<FlushWorker> logger,
        IServiceScopeFactory scopeFactory,
        IRedisConnectionFactory redisFactory,
        IWorkDistributor workDistributor, // Injected
        IRedisLockManager lockManager,     // Injected
        IConfiguration configuration,      // Injected for InstanceId
        IOptions<RedisSettings> redisSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _redisFactory = redisFactory;
        _workDistributor = workDistributor;
        _lockManager = lockManager;
        _redisSettings = redisSettings.Value;
        // TODO: Ensure InstanceId configuration is robust
        _instanceId = configuration["InstanceId"] ?? Guid.NewGuid().ToString("N"); 
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FlushWorker starting for instance {InstanceId} at: {Time}", _instanceId, DateTimeOffset.Now);

        // Register instance and get initial work assignment (handled within ProcessPendingUpdates)
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process companies assigned to this instance
                await ProcessAssignedUpdates(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("FlushWorker {InstanceId} stopping due to cancellation request.", _instanceId);
                break; // Exit loop cleanly on cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FlushWorker {InstanceId}: Unhandled error occurred in main processing loop.", _instanceId);
                // Avoid tight loop on continuous errors
            }

            // Wait before the next processing cycle
            try
            {
                await Task.Delay(_redisSettings.FlushIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                 _logger.LogInformation("FlushWorker {InstanceId} delay cancelled.", _instanceId);
                 break; // Exit loop if cancelled during delay
            }
        }
        
        _logger.LogInformation("FlushWorker {InstanceId} finished execution.", _instanceId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FlushWorker {InstanceId} stopping...", _instanceId);
        try
        {
            // Release assigned work before shutting down
            await _workDistributor.ReleaseWorkAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlushWorker {InstanceId}: Error releasing work during shutdown.", _instanceId);
        }
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("FlushWorker {InstanceId} stopped.", _instanceId);
    }

    private async Task ProcessAssignedUpdates(CancellationToken stoppingToken)
    {
        ISet<int> assignedCompanyIds;
        try
        {
            // Get companies assigned to this instance by the distributor
            assignedCompanyIds = await _workDistributor.GetAssignedWorkAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlushWorker {InstanceId}: Failed to get assigned work.", _instanceId);
            return; // Cannot proceed without work assignment
        }

        if (!assignedCompanyIds.Any())
        {
            _logger.LogDebug("FlushWorker {InstanceId}: No companies assigned in this cycle.", _instanceId);
            return;
        }

        _logger.LogInformation("FlushWorker {InstanceId}: Processing updates for {Count} assigned companies.", _instanceId, assignedCompanyIds.Count);
        var redis = _redisFactory.GetDatabase(); // Get DB connection once per cycle

        foreach (var companyId in assignedCompanyIds)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var lockKey = $"lock:company:{companyId}:flush";
            bool lockAcquired = false;
            
            try
            {
                // Try to acquire lock for the company
                lockAcquired = await _lockManager.AcquireLockAsync(lockKey, _instanceId, _companyLockExpiry, stoppingToken);

                if (!lockAcquired)
                {
                    _logger.LogDebug("FlushWorker {InstanceId}: Could not acquire lock for company {CompanyId}, likely processed by another instance.", _instanceId, companyId);
                    continue; // Skip if lock not acquired
                }

                _logger.LogDebug("FlushWorker {InstanceId}: Acquired lock for company {CompanyId}.", _instanceId, companyId);

                // --- Processing Logic (within lock) ---
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                int quantityDelta = 0;
                int totalDelta = 0;
                bool success = false;

                try
                {
                    // Atomically get and reset both deltas
                    var quantityTask = redis.StringGetSetAsync($"agg:company:{companyId}:quantity", "0");
                    var totalTask = redis.StringGetSetAsync($"agg:company:{companyId}:total", "0");
                    await Task.WhenAll(quantityTask, totalTask);

                    var quantity = await quantityTask;
                    var total = await totalTask;

                    // Check if there are any deltas to process
                    if ((!quantity.HasValue || quantity == "0") && (!total.HasValue || total == "0"))
                    {
                        _logger.LogTrace("FlushWorker {InstanceId}: No deltas found for company {CompanyId}.", _instanceId, companyId);
                        // Remove from pending set if no deltas exist anymore
                        // Corrected: Use the actual key string "agg:companies"
                        await redis.SetRemoveAsync("agg:companies", companyId.ToString()); 
                        success = true; // Mark as success to release lock without reverting
                        continue; 
                    }

                    // TryParse deltas
                    _ = int.TryParse(quantity.ToString(), out quantityDelta);
                    _ = int.TryParse(total.ToString(), out totalDelta);

                    if (quantityDelta == 0 && totalDelta == 0)
                    {
                         _logger.LogTrace("FlushWorker {InstanceId}: Zero deltas after parse for company {CompanyId}.", _instanceId, companyId);
                         await redis.SetRemoveAsync("agg:companies", companyId.ToString()); 
                         success = true;
                         continue;
                    }

                    // Update consumption in database
                    // TODO: Need to handle potential race condition if consumption record is created between check and update
                    // Consider using raw SQL with INSERT ... ON DUPLICATE KEY UPDATE or similar
                    var consumption = await dbContext.Consumptions
                        .Where(c => c.CompanyId == companyId) // Assuming CompanyId is int
                        .FirstOrDefaultAsync(stoppingToken);

                    if (consumption == null)
                    {
                        // This case needs careful handling. If the record doesn't exist, should we create it?
                        // Or does the delta imply it should exist? For now, log warning.
                        _logger.LogWarning("FlushWorker {InstanceId}: No consumption record found for company {CompanyId}. Deltas (Q:{Quantity}, T:{Total}) will be lost if not reverted.", _instanceId, companyId, quantityDelta, totalDelta);
                        // Decide on revert strategy - for now, we revert below if success is false
                    }
                    else
                    {
                        consumption.Quantity += quantityDelta;
                        consumption.Total += totalDelta;
                        await dbContext.SaveChangesAsync(stoppingToken);
                        success = true; // Mark as success
                        
                        // Remove from pending set ONLY after successful DB update
                        await redis.SetRemoveAsync("agg:companies", companyId.ToString()); 

                        _logger.LogInformation(
                            "FlushWorker {InstanceId}: Successfully applied deltas (Quantity: {Quantity}, Total: {Total}) for company {CompanyId}", 
                            _instanceId, quantityDelta, totalDelta, companyId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FlushWorker {InstanceId}: Error processing updates for company {CompanyId} within lock.", _instanceId, companyId);
                    success = false; // Ensure revert happens
                }
                finally
                {
                     // Revert Redis deltas if DB update failed
                    if (!success)
                    {
                        _logger.LogWarning("FlushWorker {InstanceId}: Reverting Redis deltas for company {CompanyId} due to processing failure.", _instanceId, companyId);
                        if (quantityDelta != 0) await redis.StringIncrementAsync($"agg:company:{companyId}:quantity", quantityDelta);
                        if (totalDelta != 0) await redis.StringIncrementAsync($"agg:company:{companyId}:total", totalDelta);
                    }
                }
                // --- End Processing Logic (within lock) ---

            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "FlushWorker {InstanceId}: Error during lock acquisition or outer processing for company {CompanyId}.", _instanceId, companyId);
                 // Lock might not have been acquired or released properly, potential issue here.
            }
            finally
            {
                // Ensure lock is released if it was acquired
                if (lockAcquired)
                {
                    await _lockManager.ReleaseLockAsync(lockKey, _instanceId, stoppingToken);
                     _logger.LogDebug("FlushWorker {InstanceId}: Released lock for company {CompanyId}.", _instanceId, companyId);
                }
            }
        } // End foreach company
    } // End ProcessAssignedUpdates
}
