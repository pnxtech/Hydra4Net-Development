using Hydra4NET;
using Microsoft.Extensions.Configuration;

namespace MessageDemo;

public class App
{
    public App()
    {
    }

    public async Task Run(string[] args)
    {
        Sender? sender = null;
        Queuer? queuer = null;

        HydraConfigObject? config = HydraConfigObject.Load("config.json");
        if (config == null)
        {
            Console.WriteLine("Hydra config.json not found");
            Environment.Exit(1);
        }

        // Create an instance of Hydra4Net using the loaded config file
        Hydra hydra = new(config);

        AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
        void AppDomain_ProcessExit(object? sender, EventArgs e)
        {
            hydra.Shutdown();
        }

        // Load the hydra config.json file
      

        // Initialize Hydra 
        await hydra.InitAsync();

        // Determine whether this instance of MessageDemo
        // should play the role of a sender or a queuer
        string role = config?.Hydra?.ServiceType ?? "unknown";
        switch (role)
        {
            case "sender":
                sender = new Sender(hydra);
                break;
            case "queuer":
                queuer = new Queuer(hydra);
                queuer.Init();
                break;
            default:
                Console.WriteLine("Hydra config.json doesn't specify a valid ServiceType role");
                Environment.Exit(1);
                break;
        }

        Console.WriteLine($"Service functioning as a {role}");
        Console.WriteLine();

        // Setup an OnMessageHandler to recieve incoming messages
        hydra.OnMessageHandler(async (msg) =>
        {
            Console.WriteLine($"Sender: received message of type {msg.Type}");
            if (sender != null && msg.MessageJson != null)
            {
                await sender.ProcessMessage(msg.Type, msg.MessageJson);
                Console.WriteLine();
            }
        });
    }
}
