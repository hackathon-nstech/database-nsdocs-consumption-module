using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using NSdocs.Domain.Entities;

namespace NSdocs.Worker.Consumers;

public class DocumentUpdatedConsumer : IConsumer<DocumentUpdatedEvent>
{
    private readonly ILogger<DocumentUpdatedConsumer> _logger;
    private readonly IApplicationDbContext _context;

    public DocumentUpdatedConsumer(
        ILogger<DocumentUpdatedConsumer> logger,
        IApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task Consume(ConsumeContext<DocumentUpdatedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation("Consuming DocumentUpdatedEvent: {DocumentId}", @event.DocumentId);
        
        // Get the current month
        var currentMonth = new DateTime(@event.RequestDate.Year, @event.RequestDate.Month, 1);
        
        // Check if status or origin changed
        if (@event.Status != @event.PreviousStatus || @event.Origin != @event.PreviousOrigin)
        {
            // Decrement previous status/origin consumption
            var previousConsumption = await _context.Consumptions
                .FirstOrDefaultAsync(c => 
                    c.CompanyId == @event.PreviousCompanyId &&
                    c.ConsumptionDate == currentMonth &&
                    c.Origin == @event.PreviousOrigin &&
                    c.DocumentType == @event.DocumentType &&
                    c.Status == @event.PreviousStatus, 
                    context.CancellationToken);
            
            if (previousConsumption != null)
            {
                previousConsumption.Quantity -= 1;
                
                _logger.LogInformation(
                    "Decremented consumption for company {CompanyId}, date {Date}, type {Type}, origin {Origin}, status {Status}: Quantity {Quantity}, Total {Total}",
                    previousConsumption.CompanyId,
                    previousConsumption.ConsumptionDate,
                    previousConsumption.DocumentType,
                    previousConsumption.Origin,
                    previousConsumption.Status,
                    previousConsumption.Quantity,
                    previousConsumption.Total);
            }
            
            // Increment new status/origin consumption
            var newConsumption = await _context.Consumptions
                .FirstOrDefaultAsync(c => 
                    c.CompanyId == @event.CompanyId &&
                    c.ConsumptionDate == currentMonth &&
                    c.Origin == @event.Origin &&
                    c.DocumentType == @event.DocumentType &&
                    c.Status == @event.Status, 
                    context.CancellationToken);
            
            if (newConsumption == null)
            {
                // Create new consumption record
                newConsumption = new Consumption
                {
                    CompanyId = @event.CompanyId,
                    ConsumptionDate = currentMonth,
                    Origin = @event.Origin,
                    DocumentType = @event.DocumentType,
                    Status = @event.Status,
                    Quantity = 1,
                    Total = 1
                };
                
                _context.Consumptions.Add(newConsumption);
                
                _logger.LogInformation(
                    "Created new consumption for company {CompanyId}, date {Date}, type {Type}, origin {Origin}, status {Status}: Quantity {Quantity}, Total {Total}",
                    newConsumption.CompanyId,
                    newConsumption.ConsumptionDate,
                    newConsumption.DocumentType,
                    newConsumption.Origin,
                    newConsumption.Status,
                    newConsumption.Quantity,
                    newConsumption.Total);
            }
            else
            {
                // Update existing consumption record
                newConsumption.Quantity += 1;
                
                _logger.LogInformation(
                    "Incremented consumption for company {CompanyId}, date {Date}, type {Type}, origin {Origin}, status {Status}: Quantity {Quantity}, Total {Total}",
                    newConsumption.CompanyId,
                    newConsumption.ConsumptionDate,
                    newConsumption.DocumentType,
                    newConsumption.Origin,
                    newConsumption.Status,
                    newConsumption.Quantity,
                    newConsumption.Total);
            }
            
            await _context.SaveChangesAsync(context.CancellationToken);
        }
    }
}
