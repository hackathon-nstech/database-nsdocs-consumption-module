using NSdocs.Flusher.Workers;
using NSdocs.Infrastructure;
using NSdocs.Infrastructure.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Add services
builder.Services
    .Configure<RedisSettings>(builder.Configuration.GetSection("Redis"))
    .AddInfrastructure(builder.Configuration);

// Add worker
builder.Services.AddHostedService<FlushWorker>();

var host = builder.Build();
host.Run();
