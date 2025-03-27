using MediatR;
using NSdocs.Domain.Enums;

namespace NSdocs.Application.Documents.Commands;

public record CreateDocumentCommand : IRequest<long>
{
    public int CompanyId { get; init; }
    public string AccessKey { get; init; } = string.Empty;
    public DocumentOrigin Origin { get; init; }
    public DocumentType DocumentType { get; init; }
    public DocumentStatus Status { get; init; }
}
