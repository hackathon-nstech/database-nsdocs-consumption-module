using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSdocs.Application.Common.Interfaces;

namespace NSdocs.Infrastructure.Services;

/// <summary>
/// RabbitMQ implementation of the event publisher using MassTransit.
/// Publishes events to RabbitMQ for asynchronous processing.
/// </summary>
public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private readonly IBus _bus;
    private readonly RabbitMQOptions _options;

    public RabbitMQEventPublisher(
        ILogger<RabbitMQEventPublisher> logger,
        IBus bus,
        IOptions<RabbitMQOptions> options)
    {
        _logger = logger;
        _bus = bus;
        _options = options.Value;
        
        _logger.LogInformation("MassTransit RabbitMQ publisher initialized with queue: {QueueName}", 
            _options.QueueName);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        try
        {
            var eventType = typeof(TEvent).Name;
            
            // Publish message using MassTransit
            await _bus.Publish(@event, cancellationToken);
            
            _logger.LogInformation(
                "Event published to RabbitMQ using MassTransit: {EventType}", 
                eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to RabbitMQ");
            throw;
        }
    }
}

public class RabbitMQOptions
{
    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "nsdocs-documents";
    public int PrefetchCount { get; set; } = 100;
}
