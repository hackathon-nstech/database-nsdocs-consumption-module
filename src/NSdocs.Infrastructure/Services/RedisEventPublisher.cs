using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using NSdocs.Infrastructure.Configuration;
using StackExchange.Redis;

namespace NSdocs.Infrastructure.Services;

public class RedisEventPublisher : IEventPublisher
{
    private readonly ILogger<RedisEventPublisher> _logger;
    private readonly IDatabase _redis;

    public RedisEventPublisher(
        IRedisConnectionFactory redisFactory,
        ILogger<RedisEventPublisher> logger)
    {
        _redis = redisFactory.GetDatabase();
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        DocumentEventBase? documentEvent = null;
        try
        {
            if (@event is not DocumentEventBase docEvent)
            {
                _logger.LogWarning("Event type {EventType} is not a DocumentEventBase", typeof(TEvent).Name);
                return;
            }

            documentEvent = docEvent;

            // Calculate deltas based on event type
            var (quantity, total) = GetDeltas(docEvent);

            if (quantity == 0 && total == 0)
            {
                _logger.LogInformation("Event type {EventType} has no consumption impact", typeof(TEvent).Name);
                return;
            }

            var companyId = documentEvent.CompanyId;

            // Increment company's aggregation values
            var tasks = new List<Task>
            {
                _redis.StringIncrementAsync($"agg:company:{companyId}:quantity", quantity),
                _redis.StringIncrementAsync($"agg:company:{companyId}:total", total),
                _redis.SetAddAsync("agg:companies", companyId)
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Successfully published deltas (Quantity: {Quantity}, Total: {Total}) for company {CompanyId}", 
                quantity,
                total,
                companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing event {EventType} for company {CompanyId}",
                typeof(TEvent).Name,
                documentEvent.CompanyId);
            throw;
        }
    }

    private static (int quantity, int total) GetDeltas<TEvent>(TEvent @event) where TEvent : DocumentEventBase
    {
        switch (@event)
        {
            case DocumentCreatedEvent:
                return (1, 1); // Increment both quantity and total by 1
            case DocumentDeletedEvent:
                return (-1, -1); // Decrement both quantity and total by 1
            case DocumentUpdatedEvent:
                return (0, 0); // No impact on counts
            default:
                throw new ArgumentException($"Unsupported event type: {typeof(TEvent).Name}");
        }
    }
}
