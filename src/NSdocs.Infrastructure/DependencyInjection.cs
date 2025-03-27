using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSdocs.Application.Common.Interfaces;
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
        
        // Register event publisher
        // Use InMemoryEventPublisher for now, will be replaced with RabbitMQEventPublisher later
        services.AddScoped<IEventPublisher, InMemoryEventPublisher>();
        
        // Uncomment to use RabbitMQ publisher
        // services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();

        return services;
    }
}
