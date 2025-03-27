using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using NSdocs.Application.Documents.Commands;
using NSdocs.Application.Documents.DTOs;
using NSdocs.Application.Documents.Queries;

namespace NSdocs.API.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/documents", async (IMediator mediator) =>
        {
            var documents = await mediator.Send(new GetDocumentsQuery());
            return Results.Ok(documents);
        })
        .WithName("GetDocuments")
        .WithOpenApi()
        .Produces<IEnumerable<DocumentListDto>>(StatusCodes.Status200OK);

        app.MapPost("/api/documents", async (CreateDocumentCommand command, IMediator mediator) =>
        {
            var id = await mediator.Send(command);
            return Results.Created($"/api/documents/{id}", id);
        })
        .WithName("CreateDocument")
        .WithOpenApi()
        .Produces<long>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        return app;
    }
}
