using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using NSdocs.Domain.Entities;

namespace NSdocs.Worker.Consumers;

public class DocumentCreatedConsumer : IConsumer<DocumentCreatedEvent>
{
    private readonly ILogger<DocumentCreatedConsumer> _logger;
    private readonly IApplicationDbContext _context;

    public DocumentCreatedConsumer(
        ILogger<DocumentCreatedConsumer> logger,
        IApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task Consume(ConsumeContext<DocumentCreatedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation("Consuming DocumentCreatedEvent: {DocumentId}", @event.DocumentId);
        
        // Get or create consumption record for the month
        var consumptionDate = new DateTime(@event.RequestDate.Year, @event.RequestDate.Month, 1);
        
        var consumption = await _context.Consumptions
            .FirstOrDefaultAsync(c => 
                c.CompanyId == @event.CompanyId &&
                c.ConsumptionDate == consumptionDate &&
                c.Origin == @event.Origin &&
                c.DocumentType == @event.DocumentType &&
                c.Status == @event.Status);
        
        if (consumption == null)
        {
            // Create new consumption record
            consumption = new Consumption
            {
                CompanyId = @event.CompanyId,
                ConsumptionDate = consumptionDate,
                Origin = @event.Origin,
                DocumentType = @event.DocumentType,
                Status = @event.Status,
                Quantity = 1,
                Total = 1
            };
            
            _context.Consumptions.Add(consumption);
        }
        else
        {
            // Update existing consumption record
            consumption.Quantity += 1;
            consumption.Total += 1;
        }
        
        await _context.SaveChangesAsync(context.CancellationToken);
        
        _logger.LogInformation(
            "Updated consumption for company {CompanyId}, date {Date}, type {Type}, origin {Origin}, status {Status}: Quantity {Quantity}, Total {Total}",
            consumption.CompanyId,
            consumption.ConsumptionDate,
            consumption.DocumentType,
            consumption.Origin,
            consumption.Status,
            consumption.Quantity,
            consumption.Total);
    }
}
