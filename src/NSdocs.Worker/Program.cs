using NSdocs.Infrastructure;

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
                services.AddInfrastructure(hostContext.Configuration);
                
                // Add worker services
                services.AddHostedService<DocumentEventConsumer>();
            });
}
