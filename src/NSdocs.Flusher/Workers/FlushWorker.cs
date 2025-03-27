using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Infrastructure.Configuration;
using NSdocs.Infrastructure.Services;
using StackExchange.Redis;

namespace NSdocs.Flusher.Workers;

public class FlushWorker : BackgroundService
{
    private readonly ILogger<FlushWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly RedisSettings _redisSettings;

    public FlushWorker(
        ILogger<FlushWorker> logger,
        IServiceScopeFactory scopeFactory,
        IRedisConnectionFactory redisFactory,
        IOptions<RedisSettings> redisSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _redisFactory = redisFactory;
        _redisSettings = redisSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FlushWorker starting at: {Time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingUpdates(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing updates");
            }

            await Task.Delay(_redisSettings.FlushIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessPendingUpdates(CancellationToken stoppingToken)
    {
        var redis = _redisFactory.GetDatabase();
        var companyIds = await redis.SetMembersAsync("agg:companies");

        if (companyIds.Length == 0)
        {
            return;
        }

        _logger.LogInformation("Processing updates for {Count} companies", companyIds.Length);

        foreach (var companyId in companyIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            int quantityDelta = 0;
            int totalDelta = 0;

            try
            {
                // Atomically get and reset both deltas
                var quantityTask = redis.StringGetSetAsync($"agg:company:{companyId}:quantity", "0");
                var totalTask = redis.StringGetSetAsync($"agg:company:{companyId}:total", "0");

                await Task.WhenAll(quantityTask, totalTask);

                var quantity = await quantityTask;
                var total = await totalTask;

                if (!quantity.HasValue && !total.HasValue)
                {
                    continue;
                }

                if (!quantity.TryParse(out quantityDelta) || !total.TryParse(out totalDelta))
                {
                    continue;
                }

                if (quantityDelta == 0 && totalDelta == 0)
                {
                    continue;
                }

                // Update consumption in database
                var consumption = await dbContext.Consumptions
                    .Where(c => c.CompanyId == int.Parse(companyId.ToString()))
                    .FirstOrDefaultAsync(stoppingToken);

                if (consumption == null)
                {
                    _logger.LogWarning("No consumption record found for company {CompanyId}", companyId);
                    continue;
                }

                consumption.Quantity += quantityDelta;
                consumption.Total += totalDelta;
                await dbContext.SaveChangesAsync(stoppingToken);

                // Remove from pending set after successful update
                await redis.SetRemoveAsync("agg:companies", companyId);

                _logger.LogInformation(
                    "Successfully applied deltas (Quantity: {Quantity}, Total: {Total}) for company {CompanyId}", 
                    quantityDelta,
                    totalDelta, 
                    companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing updates for company {CompanyId}",
                    companyId);

                // Revert both deltas if update failed
                if (quantityDelta != 0)
                {
                    await redis.StringIncrementAsync($"agg:company:{companyId}:quantity", quantityDelta);
                }
                if (totalDelta != 0)
                {
                    await redis.StringIncrementAsync($"agg:company:{companyId}:total", totalDelta);
                }
            }
        }
    }
}
