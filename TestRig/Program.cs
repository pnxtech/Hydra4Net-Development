using Microsoft.Extensions.Hosting;
using Hydra4NET;
using TestRig;

using IHost host = Host.CreateDefaultBuilder(args).Build();

Hydra hydra = new();
Tests hydraTests = new(hydra);

HydraConfigObject? config = Configuration.Load("config.json");
if (config == null)
{
    Console.WriteLine("Hydra config.json not found");
    Environment.Exit(1);
}

await hydra.Init(config);

await host.RunAsync();

/*
Console.WriteLine("HYDRA4NET Test Rig");
Console.WriteLine("===================================");
Console.WriteLine("Press Escape key to shutdown");

bool quit = false;
while (!quit)
{
    ConsoleKeyInfo ki = Console.ReadKey();
    switch (ki.Key)
    {
        case ConsoleKey.Escape:
            await hydra.Shutdown();
            quit = true;
            break;
        case ConsoleKey.F1:
            hydraTests.CreateUMFMessage();
            break;
    }
}
*/
