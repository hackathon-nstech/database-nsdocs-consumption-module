using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSdocs.Application.Common.Interfaces;

namespace NSdocs.Infrastructure.Services;

/// <summary>
/// RabbitMQ implementation of the event publisher.
/// This is a simplified version that logs the events but doesn't actually publish to RabbitMQ yet.
/// The actual RabbitMQ integration will be implemented in the next phase.
/// </summary>
public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private readonly RabbitMQOptions _options;

    public RabbitMQEventPublisher(
        ILogger<RabbitMQEventPublisher> logger,
        IOptions<RabbitMQOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        
        _logger.LogInformation("RabbitMQ publisher initialized with host: {Host}, queue: {QueueName}", 
            _options.Host, _options.QueueName);
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        var eventType = typeof(TEvent).Name;
        var message = JsonSerializer.Serialize(@event);
        
        _logger.LogInformation(
            "Event published to RabbitMQ (simulated): {EventType} - Queue: {QueueName} - Message: {Message}", 
            eventType, _options.QueueName, message);
        
        // TODO: Implement actual RabbitMQ publishing in the next phase
        // This will require adding the RabbitMQ.Client package and implementing the connection logic
        
        return Task.CompletedTask;
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
