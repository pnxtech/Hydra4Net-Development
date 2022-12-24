using Microsoft.Extensions.Hosting;
using Hydra4NET;
using TestRig;

Hydra hydra = new();
Tests hydraTests = new(hydra);

using IHost host = Host.CreateDefaultBuilder(args).Build();
AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
void AppDomain_ProcessExit(object? sender, EventArgs e)
{
    hydra.Shutdown();
}


HydraConfigObject? config = Configuration.Load("config.json");
if (config == null)
{
    Console.WriteLine("Hydra config.json not found");
    Environment.Exit(1);
}

hydra.OnMessageHandler(async (string? message) =>
{
    Console.WriteLine(message);
    await Task.Delay(1);
});

await hydra.Init(config);
await host.RunAsync();

