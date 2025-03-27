using MediatR;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Domain.Entities;
using NSdocs.Domain.Enums;

namespace NSdocs.Application.Documents.Commands;

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, long>
{
    private readonly IApplicationDbContext _context;

    public CreateDocumentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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

        return document.Id;
    }
}
