using NSdocs.API.Endpoints;
using NSdocs.Application;
using NSdocs.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSwagger(c =>
{
    c.RouteTemplate = "docs/{documentName}/swagger.json";
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/docs/v1/swagger.json", "NSdocs API V1");
    c.RoutePrefix = "docs";
});

app.MapGet("/", () => "NSdocs Document Consumption Module API");
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.MapDocumentEndpoints();

app.Run();
