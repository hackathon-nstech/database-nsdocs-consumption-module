using MediatR;
using Microsoft.EntityFrameworkCore;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;

namespace NSdocs.Application.Documents.Commands;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public DeleteDocumentCommandHandler(
        IApplicationDbContext context,
        IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (document == null)
        {
            return false;
        }

        // Store document data for the event before removing it
        var documentEvent = new DocumentDeletedEvent
        {
            DocumentId = document.Id,
            CompanyId = document.CompanyId,
            AccessKey = document.AccessKey,
            RequestDate = document.RequestDate,
            Origin = document.Origin,
            DocumentType = document.DocumentType,
            Status = document.Status
        };

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);

        // Publish document deleted event
        await _eventPublisher.PublishAsync(documentEvent, cancellationToken);

        return true;
    }
}
