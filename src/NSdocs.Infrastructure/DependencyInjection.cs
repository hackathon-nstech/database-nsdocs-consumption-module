using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Add missing using directive
using NSdocs.Application.Common.Interfaces;
using NSdocs.Application.Documents.Events;
using NSdocs.Infrastructure.Configuration;
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
            
        // Configure Redis options
        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        
        // Register Redis services
        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
        services.AddScoped<IEventPublisher, RedisEventPublisher>();
        services.AddSingleton<IRedisLockManager, RedisLockManager>(); 
        
        // Register Work Distributor - Instance ID needs proper configuration later
        // For now, using a temporary default or allowing DI to resolve if configured elsewhere
        services.AddSingleton<IWorkDistributor>(provider => 
        {
            var redisFactory = provider.GetRequiredService<IRedisConnectionFactory>();
            var lockManager = provider.GetRequiredService<IRedisLockManager>();
            var logger = provider.GetRequiredService<ILogger<WorkDistributor>>();
            // TODO: Replace "default-instance" with actual instance ID from config/env
            var instanceId = configuration["InstanceId"] ?? Guid.NewGuid().ToString("N"); 
            return new WorkDistributor(redisFactory, lockManager, logger, instanceId);
        });

        // Register Health Monitor
        services.AddSingleton<IHealthMonitor>(provider =>
        {
            var redisFactory = provider.GetRequiredService<IRedisConnectionFactory>();
            var workDistributor = provider.GetRequiredService<IWorkDistributor>();
            var logger = provider.GetRequiredService<ILogger<HealthMonitor>>();
            // TODO: Replace "default-instance" with actual instance ID from config/env
            var instanceId = configuration["InstanceId"] ?? configuration["HOSTNAME"] ?? Guid.NewGuid().ToString("N"); // Read InstanceId or HOSTNAME env var

            // Read intervals from configuration, providing defaults if not found
            var heartbeatSeconds = configuration.GetValue<int?>("FlusherSettings:HeartbeatIntervalSeconds") ?? 30;
            var expiryMinutes = configuration.GetValue<int?>("FlusherSettings:InstanceExpiryMinutes") ?? 5;
            
            var heartbeatInterval = TimeSpan.FromSeconds(heartbeatSeconds);
            var instanceExpiry = TimeSpan.FromMinutes(expiryMinutes);

            logger.LogInformation("Registering HealthMonitor for instance {InstanceId} with Heartbeat: {HeartbeatInterval}, Expiry: {InstanceExpiry}",
                instanceId, heartbeatInterval, instanceExpiry);

            return new HealthMonitor(redisFactory, workDistributor, logger, instanceId, heartbeatInterval, instanceExpiry);
        });
        
        return services;
    }
}
