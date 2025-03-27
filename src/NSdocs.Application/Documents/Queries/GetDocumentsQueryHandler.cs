using MediatR;
using Microsoft.EntityFrameworkCore;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.DTOs;

namespace NSdocs.Application.Documents.Queries;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, IEnumerable<DocumentListDto>>
{
    private readonly IApplicationDbContext _context;
    
    public GetDocumentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<DocumentListDto>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Documents
            .Select(d => new DocumentListDto
            {
                Id = d.Id,
                AccessKey = d.AccessKey,
                DocumentType = d.DocumentType.ToString().ToLower(),
                Status = d.Status.ToString().ToLower()
            })
            .ToListAsync(cancellationToken);
    }
}
