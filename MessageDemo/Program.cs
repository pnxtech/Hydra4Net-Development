using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MessageDemo;

class Program
{
    static async Task Main(string[] args)
    {
        using IHost host = CreateHostBuilder(args).Build();
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try 
        {
            await services.GetRequiredService<App>().Run(args);
        }
        catch (Exception ex) 
        { 
            Console.WriteLine(ex.Message);
        }

        static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<App>();
                });
        }

        // Prevent app from closing
        await host.RunAsync();
    }
}