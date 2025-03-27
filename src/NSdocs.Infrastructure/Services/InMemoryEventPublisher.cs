using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;

namespace NSdocs.Infrastructure.Services;

public class InMemoryEventPublisher : IEventPublisher
{
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        _logger.LogInformation("Event published: {EventType} - {EventData}", typeof(TEvent).Name, @event);
        return Task.CompletedTask;
    }
}
