using MediatR;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using NSdocs.Domain.Entities;
using NSdocs.Domain.Enums;

namespace NSdocs.Application.Documents.Commands;

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, long>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public CreateDocumentCommandHandler(
        IApplicationDbContext context,
        IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<long> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = new Document
        {
            CompanyId = request.CompanyId,
            AccessKey = request.AccessKey,
            Origin = request.Origin,
            DocumentType = request.DocumentType,
            Status = request.Status,
            RequestDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        // Publish document created event
        await _eventPublisher.PublishAsync(new DocumentCreatedEvent
        {
            DocumentId = document.Id,
            CompanyId = document.CompanyId,
            AccessKey = document.AccessKey,
            RequestDate = document.RequestDate,
            Origin = document.Origin,
            DocumentType = document.DocumentType,
            Status = document.Status
        }, cancellationToken);

        return document.Id;
    }
}
