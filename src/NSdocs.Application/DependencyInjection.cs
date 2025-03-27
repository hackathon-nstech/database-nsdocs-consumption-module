using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;
using FluentValidation;
using NSdocs.Application.Common.Behaviors;

namespace NSdocs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            
            // Register validation behavior
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });
        
        // Register all validators
        RegisterValidators(services);

        return services;
    }
    
    private static void RegisterValidators(IServiceCollection services)
    {
        // Scan for all validators in the assembly and register them
        var assembly = Assembly.GetExecutingAssembly();
        var validatorType = typeof(IValidator<>);
        
        var validatorTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorType));
        
        foreach (var validator in validatorTypes)
        {
            var validatorInterface = validator.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorType);
            
            services.AddTransient(validatorInterface, validator);
        }
    }
}
