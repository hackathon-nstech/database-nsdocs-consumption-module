using MediatR;
using Microsoft.EntityFrameworkCore;
using NSdocs.Application.Common.Interfaces;

namespace NSdocs.Application.Documents.Commands;

public class UpdateDocumentCommandHandler : IRequestHandler<UpdateDocumentCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateDocumentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (document == null)
        {
            return false;
        }

        document.CompanyId = request.CompanyId;
        document.AccessKey = request.AccessKey;
        document.Origin = request.Origin;
        document.DocumentType = request.DocumentType;
        document.Status = request.Status;
        document.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
