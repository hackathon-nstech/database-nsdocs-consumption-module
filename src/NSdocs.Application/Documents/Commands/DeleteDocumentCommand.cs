using MediatR;

namespace NSdocs.Application.Documents.Commands;

public record DeleteDocumentCommand : IRequest<bool>
{
    public long Id { get; init; }
}
