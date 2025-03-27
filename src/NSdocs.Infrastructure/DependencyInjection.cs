using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using NSdocs.Infrastructure.Data;
using NSdocs.Infrastructure.Services;

namespace NSdocs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
            
        // Configure RabbitMQ options
        services.Configure<RabbitMQOptions>(configuration.GetSection("RabbitMQ"));
        
        // Configure MassTransit
        ConfigureMassTransit(services, configuration);
        
        // Register event publisher
        services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();

        return services;
    }
    
    private static void ConfigureMassTransit(IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqConfig = configuration.GetSection("RabbitMQ");
        
        services.AddMassTransit(x =>
        {
            // Register document event consumers
            x.SetKebabCaseEndpointNameFormatter();
            
            // Configure RabbitMQ
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqConfig["Host"], h =>
                {
                    h.Username(rabbitMqConfig["Username"]);
                    h.Password(rabbitMqConfig["Password"]);
                });
                
                // Configure message topology
                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
