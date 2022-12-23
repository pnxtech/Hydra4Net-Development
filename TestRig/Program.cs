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

hydra.OnMessageHandler((string? message) =>
{
    Console.WriteLine(message);
});
await hydra.Init(config);

await host.RunAsync();
