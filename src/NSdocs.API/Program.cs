using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using NSdocs.API.Endpoints;
using NSdocs.API.Middleware;
using NSdocs.Application;
using NSdocs.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON serialization options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

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

// Add exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/", () => "NSdocs Document Consumption Module API");
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.MapDocumentEndpoints();

app.Run();
