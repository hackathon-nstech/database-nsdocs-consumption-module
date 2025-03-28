using MediatR;
using NSdocs.Domain.Enums;

namespace NSdocs.Application.Documents.Commands;

public record UpdateDocumentCommand : IRequest<bool>
{
    public long Id { get; set; }
    public int CompanyId { get; init; }
    public string AccessKey { get; init; } = string.Empty;
    public DocumentOrigin Origin { get; init; }
    public DocumentType DocumentType { get; init; }
    public DocumentStatus Status { get; init; }
}
