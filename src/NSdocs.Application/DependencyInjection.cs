using Microsoft.Extensions.DependencyInjection;

namespace NSdocs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Future: Add MediatR, FluentValidation, and other application-level services
        return services;
    }
}
