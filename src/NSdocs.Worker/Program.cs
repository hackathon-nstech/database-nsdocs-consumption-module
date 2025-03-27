using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using NSdocs.Infrastructure.Data;
using NSdocs.Infrastructure.Services;
using NSdocs.Worker.Consumers;

namespace NSdocs.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Add infrastructure services
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseMySql(
                        hostContext.Configuration.GetConnectionString("DefaultConnection"), 
                        ServerVersion.AutoDetect(hostContext.Configuration.GetConnectionString("DefaultConnection"))));
                
                services.AddScoped<IApplicationDbContext>(provider =>
                    provider.GetRequiredService<ApplicationDbContext>());
                
                // Configure RabbitMQ options
                services.Configure<RabbitMQOptions>(hostContext.Configuration.GetSection("RabbitMQ"));
                
                // Configure MassTransit
                ConfigureMassTransit(services, hostContext.Configuration);
            });
            
    private static void ConfigureMassTransit(IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqConfig = configuration.GetSection("RabbitMQ");
        
        services.AddMassTransit(x =>
        {
            // Register document event consumers
            x.AddConsumer<DocumentCreatedConsumer>();
            x.AddConsumer<DocumentUpdatedConsumer>();
            x.AddConsumer<DocumentDeletedConsumer>();
            
            x.SetKebabCaseEndpointNameFormatter();
            
            // Configure RabbitMQ
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqConfig["Host"], h =>
                {
                    h.Username(rabbitMqConfig["Username"]);
                    h.Password(rabbitMqConfig["Password"]);
                });
                
                // Configure consumers
                cfg.ReceiveEndpoint(rabbitMqConfig["QueueName"], e =>
                {
                    e.PrefetchCount = int.Parse(rabbitMqConfig["PrefetchCount"] ?? "100");
                    e.ConfigureConsumer<DocumentCreatedConsumer>(context);
                    e.ConfigureConsumer<DocumentUpdatedConsumer>(context);
                    e.ConfigureConsumer<DocumentDeletedConsumer>(context);
                });
            });
        });
    }
}
