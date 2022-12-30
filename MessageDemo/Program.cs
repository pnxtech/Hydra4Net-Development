using Hydra4NET;
using Microsoft.Extensions.Hosting;

namespace MessageDemo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Create an instance of Hydra4Net
            Hydra hydra = new();

            // Create a Host instance to prevent this console app
            // from closing and to track application close
            using IHost host = Host.CreateDefaultBuilder(args).Build();
            AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
            void AppDomain_ProcessExit(object? sender, EventArgs e)
            {
                hydra.Shutdown();
            }

            // Load the hydra config.json file
            //
            HydraConfigObject? config = Configuration.Load("config.json");
            if (config == null)
            {
                Console.WriteLine("Hydra config.json not found");
                Environment.Exit(1);
            }

            // Setup an OnMessageHandler to recieve incoming UMF messages
            //
            hydra.OnMessageHandler(async (string type, string? message) =>
            {
                Console.WriteLine($"{type}: {message}");
                await Task.Delay(1);
            });

            // Initialize Hydra using the loaded config file
            await hydra.Init(config);

            // Prevent app from closing
            await host.RunAsync();
        }
    }
}