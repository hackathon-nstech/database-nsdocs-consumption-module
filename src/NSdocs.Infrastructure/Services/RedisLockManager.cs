using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;
using StackExchange.Redis;

namespace NSdocs.Infrastructure.Services;

public class RedisLockManager : IRedisLockManager
{
    private readonly IDatabase _redis;
    private readonly ILogger<RedisLockManager> _logger;

    // Lua script for safe lock release (check identifier before deleting)
    private const string ReleaseLockScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    // Lua script for safe lock extension (check identifier before extending)
    private const string ExtendLockScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('pexpire', KEYS[1], ARGV[2])
        else
            return 0
        end";

    // No need for static LuaScript fields when evaluating raw strings
    // private static LuaScript? _releaseLockLuaScript;
    // private static LuaScript? _extendLockLuaScript;

    public RedisLockManager(IRedisConnectionFactory redisFactory, ILogger<RedisLockManager> logger)
    {
        _redis = redisFactory.GetDatabase();
        _logger = logger;

        // No need to prepare scripts here anymore
    }

    public async Task<bool> AcquireLockAsync(string resourceKey, string lockIdentifier, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            // SET resourceKey lockIdentifier PX expiryMilliseconds NX
            var acquired = await _redis.StringSetAsync(resourceKey, lockIdentifier, expiry, When.NotExists, CommandFlags.None);
            if (acquired)
            {
                _logger.LogDebug("Lock acquired for resource '{ResourceKey}' by identifier '{LockIdentifier}' with expiry {Expiry}", resourceKey, lockIdentifier, expiry);
            }
            else
            {
                _logger.LogDebug("Failed to acquire lock for resource '{ResourceKey}' by identifier '{LockIdentifier}' (already held)", resourceKey, lockIdentifier);
            }
            return acquired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for resource '{ResourceKey}' by identifier '{LockIdentifier}'", resourceKey, lockIdentifier);
            return false; // Assume lock not acquired on error
        }
    }

    public async Task<bool> ReleaseLockAsync(string resourceKey, string lockIdentifier, CancellationToken cancellationToken = default)
    {
        try
        {
            // KEYS[1] = resourceKey
            // ARGV[1] = lockIdentifier
            var result = await _redis.ScriptEvaluateAsync(
                ReleaseLockScript, // Evaluate raw script string
                keys: new RedisKey[] { resourceKey }, 
                values: new RedisValue[] { lockIdentifier });
            
            if (result is RedisResult redisResult && (long)redisResult == 1)
            {
                _logger.LogDebug("Lock released for resource '{ResourceKey}' by identifier '{LockIdentifier}'", resourceKey, lockIdentifier);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to release lock for resource '{ResourceKey}'. Identifier '{LockIdentifier}' did not match or lock expired.", resourceKey, lockIdentifier);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for resource '{ResourceKey}' by identifier '{LockIdentifier}'", resourceKey, lockIdentifier);
            return false; // Assume lock not released on error
        }
    }

    public async Task<bool> ExtendLockAsync(string resourceKey, string lockIdentifier, TimeSpan newExpiry, CancellationToken cancellationToken = default)
    {
        try
        {
            // KEYS[1] = resourceKey
            // ARGV[1] = lockIdentifier
            // ARGV[2] = expiryMilliseconds
            var expiryMilliseconds = (long)newExpiry.TotalMilliseconds;
            var result = await _redis.ScriptEvaluateAsync(
                ExtendLockScript, // Evaluate raw script string
                keys: new RedisKey[] { resourceKey }, 
                values: new RedisValue[] { lockIdentifier, expiryMilliseconds });

            if (result is RedisResult redisResult && (long)redisResult == 1)
            {
                _logger.LogDebug("Lock extended for resource '{ResourceKey}' by identifier '{LockIdentifier}' to {NewExpiry}", resourceKey, lockIdentifier, newExpiry);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to extend lock for resource '{ResourceKey}'. Identifier '{LockIdentifier}' did not match or lock expired.", resourceKey, lockIdentifier);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending lock for resource '{ResourceKey}' by identifier '{LockIdentifier}'", resourceKey, lockIdentifier);
            return false; // Assume lock not extended on error
        }
    }

    public async Task<bool> IsLockHeldAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _redis.KeyExistsAsync(resourceKey);
            _logger.LogDebug("Checked lock status for resource '{ResourceKey}'. Held: {IsHeld}", resourceKey, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking lock status for resource '{ResourceKey}'", resourceKey);
            return false; // Assume lock not held on error
        }
    }
}
