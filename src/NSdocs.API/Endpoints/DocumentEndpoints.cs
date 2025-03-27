using Microsoft.EntityFrameworkCore;
using NSdocs.Infrastructure.Data;

namespace NSdocs.API.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/documents", async (ApplicationDbContext db) =>
        {
            var documents = await db.Documents.ToListAsync();
            return Results.Ok(documents);
        })
        .WithName("GetDocuments")
        .WithOpenApi();

        return app;
    }
}
