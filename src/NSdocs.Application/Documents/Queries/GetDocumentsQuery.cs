using MediatR;
using NSdocs.Application.Documents.DTOs;

namespace NSdocs.Application.Documents.Queries;

public record GetDocumentsQuery : IRequest<IEnumerable<DocumentListDto>>;
