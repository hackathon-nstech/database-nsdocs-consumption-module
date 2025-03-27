using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NSdocs.Worker;

public class DocumentEventConsumer : BackgroundService
{
    private readonly ILogger<DocumentEventConsumer> _logger;
    private readonly IApplicationDbContext _context;

    public DocumentEventConsumer(
        ILogger<DocumentEventConsumer> logger,
        IApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Event Consumer started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            // This will be replaced with actual RabbitMQ consumer logic
            // For now, just a placeholder
            
            _logger.LogInformation("Document Event Consumer running at: {time}", DateTimeOffset.Now);
            
            await Task.Delay(1000, stoppingToken);
        }
    }

    // These methods will be implemented when we add RabbitMQ integration
    
    private Task HandleDocumentCreatedEvent(DocumentCreatedEvent @event)
    {
        _logger.LogInformation("Handling DocumentCreatedEvent: {DocumentId}", @event.DocumentId);
        // Update consumption logic will be implemented here
        return Task.CompletedTask;
    }
    
    private Task HandleDocumentUpdatedEvent(DocumentUpdatedEvent @event)
    {
        _logger.LogInformation("Handling DocumentUpdatedEvent: {DocumentId}", @event.DocumentId);
        // Update consumption logic will be implemented here
        return Task.CompletedTask;
    }
    
    private Task HandleDocumentDeletedEvent(DocumentDeletedEvent @event)
    {
        _logger.LogInformation("Handling DocumentDeletedEvent: {DocumentId}", @event.DocumentId);
        // Update consumption logic will be implemented here
        return Task.CompletedTask;
    }
}
