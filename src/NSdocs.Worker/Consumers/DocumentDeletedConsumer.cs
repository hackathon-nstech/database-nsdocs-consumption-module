using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;

namespace NSdocs.Worker.Consumers;

public class DocumentDeletedConsumer : IConsumer<DocumentDeletedEvent>
{
    private readonly ILogger<DocumentDeletedConsumer> _logger;
    private readonly IApplicationDbContext _context;

    public DocumentDeletedConsumer(
        ILogger<DocumentDeletedConsumer> logger,
        IApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task Consume(ConsumeContext<DocumentDeletedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation("Consuming DocumentDeletedEvent: {DocumentId}", @event.DocumentId);
        
        // Get the month of the document
        var documentMonth = new DateTime(@event.RequestDate.Year, @event.RequestDate.Month, 1);
        
        // Decrement consumption
        var consumption = await _context.Consumptions
            .FirstOrDefaultAsync(c => 
                c.CompanyId == @event.CompanyId &&
                c.ConsumptionDate == documentMonth &&
                c.Origin == @event.Origin &&
                c.DocumentType == @event.DocumentType &&
                c.Status == @event.Status, 
                context.CancellationToken);
        
        if (consumption != null)
        {
            consumption.Quantity -= 1;
            
            _logger.LogInformation(
                "Decremented consumption for company {CompanyId}, date {Date}, type {Type}, origin {Origin}, status {Status}: Quantity {Quantity}, Total {Total}",
                consumption.CompanyId,
                consumption.ConsumptionDate,
                consumption.DocumentType,
                consumption.Origin,
                consumption.Status,
                consumption.Quantity,
                consumption.Total);
            
            await _context.SaveChangesAsync(context.CancellationToken);
        }
    }
}
