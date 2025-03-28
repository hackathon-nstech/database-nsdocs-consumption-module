using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Globalization; // For DateOnly parsing
using System.Text.RegularExpressions; // For key parsing
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Domain.Entities; // Added for Consumption entity
using NSdocs.Domain.Enums; // Added for Enums
using NSdocs.Infrastructure.Configuration;
using NSdocs.Infrastructure.Services;
using StackExchange.Redis;

namespace NSdocs.Flusher.Workers;

public class FlushWorker : BackgroundService
{
    private const string PendingFlushSetKey = "agg:pending_flush";
    private readonly ILogger<FlushWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly IRedisLockManager _lockManager;
    private readonly string _instanceId;
    private readonly RedisSettings _redisSettings;
    private readonly TimeSpan _baseKeyLockExpiry = TimeSpan.FromMinutes(1); // How long to lock a base key for processing

    public FlushWorker(
        ILogger<FlushWorker> logger,
        IServiceScopeFactory scopeFactory,
        IRedisConnectionFactory redisFactory,
        // IWorkDistributor workDistributor, // Removed
        IRedisLockManager lockManager,
        IConfiguration configuration,
        IOptions<RedisSettings> redisSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _redisFactory = redisFactory;
        // _workDistributor = workDistributor; // Removed
        _lockManager = lockManager;
        _redisSettings = redisSettings.Value;
        _instanceId = configuration["InstanceId"] ?? Guid.NewGuid().ToString("N");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FlushWorker starting for instance {InstanceId} at: {Time}", _instanceId, DateTimeOffset.Now);

        // No instance registration needed here anymore as work distribution is removed

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process keys from the pending flush set
                await ProcessPendingFlushSet(stoppingToken);
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

    // Removed StopAsync override related to IWorkDistributor

    private async Task ProcessPendingFlushSet(CancellationToken stoppingToken)
    {
        var redis = _redisFactory.GetDatabase();
        _logger.LogDebug("FlushWorker {InstanceId}: Checking for pending updates in {SetKey}.", _instanceId, PendingFlushSetKey);

        // Process keys one by one using SRANDMEMBER + SREM for safety
        // Consider adding a limit to how many keys are processed per cycle to avoid long runs
        int processedCount = 0;
        int maxProcessPerCycle = 1000; // Configurable limit

        while (!stoppingToken.IsCancellationRequested && processedCount < maxProcessPerCycle)
        {
            RedisValue baseKeyValue = await redis.SetRandomMemberAsync(PendingFlushSetKey);

            if (baseKeyValue.IsNullOrEmpty)
            {
                _logger.LogTrace("FlushWorker {InstanceId}: No more keys found in {SetKey}.", _instanceId, PendingFlushSetKey);
                break; // No more keys in the set
            }

            string baseKey = baseKeyValue.ToString();
            string lockKey = $"lock:{baseKey}:flush"; // Lock based on the specific aggregation key
            bool lockAcquired = false;
            int quantityDelta = 0;
            int totalDelta = 0;

            try
            {
                lockAcquired = await _lockManager.AcquireLockAsync(lockKey, _instanceId, _baseKeyLockExpiry, stoppingToken);

                if (!lockAcquired)
                {
                    _logger.LogDebug("FlushWorker {InstanceId}: Could not acquire lock for key {BaseKey}, likely processed by another instance.", _instanceId, baseKey);
                    // Don't increment processedCount, just try another key
                    await Task.Delay(50, stoppingToken); // Small delay to prevent tight loop on contention
                    continue;
                }

                _logger.LogDebug("FlushWorker {InstanceId}: Acquired lock for key {BaseKey}.", _instanceId, baseKey);
                processedCount++; // Increment count as we are processing this key

                // --- Processing Logic (within lock) ---
                bool success = false;
                try
                {
                    // 1. Parse dimensions from baseKey
                    var (parseSuccess, companyId, consumptionDate, origin, docType, status) = ParseBaseKey(baseKey);
                    if (!parseSuccess)
                    {
                        _logger.LogError("FlushWorker {InstanceId}: Failed to parse base key {BaseKey}. Removing from set.", _instanceId, baseKey);
                        await redis.SetRemoveAsync(PendingFlushSetKey, baseKey); // Remove invalid key
                        success = true; // Mark as success to release lock without revert
                        continue;
                    }

                    // 2. Atomically get and reset granular deltas
                    var quantityKey = $"{baseKey}:quantity";
                    var totalKey = $"{baseKey}:total";
                    var quantityTask = redis.StringGetSetAsync(quantityKey, "0");
                    var totalTask = redis.StringGetSetAsync(totalKey, "0");
                    await Task.WhenAll(quantityTask, totalTask);

                    var quantityVal = await quantityTask;
                    var totalVal = await totalTask;

                    _ = int.TryParse(quantityVal.ToString(), out quantityDelta);
                    _ = int.TryParse(totalVal.ToString(), out totalDelta);

                    if (quantityDelta == 0 && totalDelta == 0)
                    {
                        _logger.LogTrace("FlushWorker {InstanceId}: Zero deltas found for key {BaseKey}. Removing from set.", _instanceId, baseKey);
                        await redis.SetRemoveAsync(PendingFlushSetKey, baseKey);
                        success = true;
                        continue;
                    }

                    // 3. Update Database
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    // Convert DateOnly to DateTime (start of day) for comparison
                    var consumptionDateTime = consumptionDate.ToDateTime(TimeOnly.MinValue);

                    var consumption = await dbContext.Consumptions
                        .FirstOrDefaultAsync(c =>
                            c.CompanyId == companyId &&
                            c.ConsumptionDate == consumptionDateTime && // Compare DateTime with DateTime
                            c.Origin == origin &&
                            c.DocumentType == docType &&
                            c.Status == status,
                            stoppingToken);

                    if (consumption == null)
                    {
                        // Create new record
                        // Convert DateOnly to DateTime (start of day) for the entity
                        consumption = new Consumption
                        {
                            CompanyId = companyId,
                            ConsumptionDate = consumptionDate.ToDateTime(TimeOnly.MinValue), // Assign DateTime
                            Origin = origin,
                            DocumentType = docType,
                            Status = status,
                            Quantity = quantityDelta,
                            Total = totalDelta
                        };
                        dbContext.Consumptions.Add(consumption);
                        _logger.LogDebug("FlushWorker {InstanceId}: Creating new consumption record for key {BaseKey}.", _instanceId, baseKey);
                    }
                    else
                    {
                        // Update existing record
                        consumption.Quantity += quantityDelta;
                        consumption.Total += totalDelta;
                        _logger.LogDebug("FlushWorker {InstanceId}: Updating existing consumption record for key {BaseKey}.", _instanceId, baseKey);
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    success = true; // DB update successful

                    // 4. Remove base key from set ONLY after successful DB update
                    await redis.SetRemoveAsync(PendingFlushSetKey, baseKey);

                    _logger.LogInformation(
                        "FlushWorker {InstanceId}: Successfully applied deltas (Q: {Quantity}, T: {Total}) for key {BaseKey}",
                        _instanceId, quantityDelta, totalDelta, baseKey);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FlushWorker {InstanceId}: Error processing updates for key {BaseKey} within lock.", _instanceId, baseKey);
                    success = false; // Ensure revert happens
                }
                finally
                {
                    // 5. Revert Redis deltas if DB update failed
                    if (!success)
                    {
                        _logger.LogWarning("FlushWorker {InstanceId}: Reverting Redis deltas for key {BaseKey} due to processing failure.", _instanceId, baseKey);
                        // Use INCRBY to revert
                        if (quantityDelta != 0) await redis.StringIncrementAsync($"{baseKey}:quantity", -quantityDelta);
                        if (totalDelta != 0) await redis.StringIncrementAsync($"{baseKey}:total", -totalDelta);
                        // Do NOT remove from PendingFlushSetKey here, let it be retried later
                    }
                }
                // --- End Processing Logic (within lock) ---
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FlushWorker {InstanceId}: Error during lock acquisition or outer processing for key {BaseKey}.", _instanceId, baseKey);
                // Lock might not have been acquired or released properly.
            }
            finally
            {
                // Ensure lock is released if it was acquired
                if (lockAcquired)
                {
                    await _lockManager.ReleaseLockAsync(lockKey, _instanceId, stoppingToken);
                    _logger.LogDebug("FlushWorker {InstanceId}: Released lock for key {BaseKey}.", _instanceId, baseKey);
                }
            }
        } // End while loop processing keys

        if (processedCount > 0)
        {
             _logger.LogInformation("FlushWorker {InstanceId}: Processed {Count} keys in this cycle.", _instanceId, processedCount);
        }
    }

    // Helper to parse the base key: "agg:company:{companyId}:{date}:{origin}:{type}:{status}"
    private (bool Success, int CompanyId, DateOnly ConsumptionDate, DocumentOrigin Origin, DocumentType DocType, DocumentStatus Status) ParseBaseKey(string baseKey)
    {
        // Example: agg:company:123:2025-03-28:file:nfe:ok
        var parts = baseKey.Split(':');
        if (parts.Length != 7 || parts[0] != "agg" || parts[1] != "company")
        {
            return (false, 0, default, default, default, default);
        }

        if (!int.TryParse(parts[2], out var companyId)) return (false, 0, default, default, default, default);
        if (!DateOnly.TryParseExact(parts[3], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var consumptionDate)) return (false, 0, default, default, default, default);
        if (!TryParseEnum<DocumentOrigin>(parts[4], out var origin)) return (false, 0, default, default, default, default);
        if (!TryParseEnum<DocumentType>(parts[5], out var docType)) return (false, 0, default, default, default, default);
        if (!TryParseEnum<DocumentStatus>(parts[6], out var status)) return (false, 0, default, default, default, default);

        return (true, companyId, consumptionDate, origin, docType, status);
    }

    // Helper to parse kebab-case string back to Enum
    private static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct, Enum
    {
        // Convert kebab-case back to PascalCase for Enum.TryParse
        // Simple approach: remove hyphens, capitalize first letter, then try parse ignoring case
        // Example: "nfe" -> "Nfe", "document-type" -> "DocumentType"
        // This might not be perfect for all cases but covers simple ones.
        // A more robust approach might involve mapping if needed.

        // Let's assume the publisher used ToLowerInvariant for acronyms (nfe) and hyphenated for others (document-status)
        // We need to handle both back to PascalCase

        string pascalCaseValue;
        if (!value.Contains('-'))
        {
             // Assume acronym like "nfe", just capitalize first letter? No, Enum.TryParse ignores case.
             // Let's rely on IgnoreCase parsing.
             pascalCaseValue = value; // Keep as is, rely on IgnoreCase
        }
        else
        {
            // Handle kebab-case like "document-status"
            var parts = value.Split('-');
            pascalCaseValue = string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
        }

        // Use Enum.TryParse with ignoreCase: true
        if (Enum.TryParse<TEnum>(pascalCaseValue, ignoreCase: true, out result))
        {
            return true;
        }

        // Fallback: Try direct match (if kebab-case somehow matches enum name directly)
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out result))
        {
             return true;
        }

        // Log failure?
        // Console.WriteLine($"Failed to parse '{value}' (tried '{pascalCaseValue}') as {typeof(TEnum).Name}");
        return false;
    }
}
