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

        app.MapPut("/api/documents/{id}", async (long id, UpdateDocumentCommand command, IMediator mediator) =>
        {
            command.Id = id;
            var result = await mediator.Send(command);
            
            if (!result)
            {
                return Results.NotFound();
            }
            
            return Results.NoContent();
        })
        .WithName("UpdateDocument")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        app.MapDelete("/api/documents/{id}", async (long id, IMediator mediator) =>
        {
            var command = new DeleteDocumentCommand { Id = id };
            var result = await mediator.Send(command);
            
            if (!result)
            {
                return Results.NotFound();
            }
            
            return Results.NoContent();
        })
        .WithName("DeleteDocument")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
