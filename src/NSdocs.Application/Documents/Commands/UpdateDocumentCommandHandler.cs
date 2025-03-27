using MediatR;
using Microsoft.EntityFrameworkCore;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using NSdocs.Domain.Enums;

namespace NSdocs.Application.Documents.Commands;

public class UpdateDocumentCommandHandler : IRequestHandler<UpdateDocumentCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public UpdateDocumentCommandHandler(
        IApplicationDbContext context,
        IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<bool> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (document == null)
        {
            return false;
        }

        // Store previous values for the event
        var previousCompanyId = document.CompanyId;
        var previousOrigin = document.Origin;
        var previousStatus = document.Status;

        document.CompanyId = request.CompanyId;
        document.AccessKey = request.AccessKey;
        document.Origin = request.Origin;
        document.DocumentType = request.DocumentType;
        document.Status = request.Status;
        document.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Only publish event if relevant fields changed
        if (previousCompanyId != document.CompanyId || 
            previousOrigin != document.Origin || 
            previousStatus != document.Status)
        {
            await _eventPublisher.PublishAsync(new DocumentUpdatedEvent
            {
                DocumentId = document.Id,
                CompanyId = document.CompanyId,
                AccessKey = document.AccessKey,
                RequestDate = document.RequestDate,
                Origin = document.Origin,
                DocumentType = document.DocumentType,
                Status = document.Status,
                PreviousCompanyId = previousCompanyId,
                PreviousOrigin = previousOrigin,
                PreviousStatus = previousStatus
            }, cancellationToken);
        }

        return true;
    }
}
