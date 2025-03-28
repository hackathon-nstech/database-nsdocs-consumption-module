using Microsoft.Extensions.Hosting; // Added for Host
using NSdocs.Flusher.Services; // Added for HealthMonitorService
using NSdocs.Flusher.Workers;
using NSdocs.Infrastructure;
using NSdocs.Infrastructure.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Add services
builder.Services
    .Configure<RedisSettings>(builder.Configuration.GetSection("Redis"))
    .AddInfrastructure(builder.Configuration);

// Add workers/hosted services
builder.Services.AddHostedService<FlushWorker>();
builder.Services.AddHostedService<HealthMonitorService>(); // Register Health Monitor Service

var host = builder.Build();
host.Run();
