using Hydra4NET;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TestRig;


// Load the hydra config.json file
//
HydraConfigObject? config = HydraConfigObject.Load("config.json");
if (config == null)
{
    Console.WriteLine("Hydra config.json not found");
    Environment.Exit(1);
}


// Create an instance of Hydra4Net using the loaded config file
Hydra hydra = new(config);

// Create an instance of a Test class for testing
// hydra functions during development
Tests hydraTests = new(hydra);

// Create a Host instance to prevent this console app
// from closing and to track application close
using IHost host = Host.CreateDefaultBuilder(args).Build();
AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
void AppDomain_ProcessExit(object? sender, EventArgs e)
{
    hydra.Shutdown();
}

// Setup an OnMessageHandler to recieve incoming UMF messages
//
hydra.OnMessageHandler(async (IInboundMessage msg) =>
{
    Console.WriteLine($"{msg.Type}: {msg.MessageJson}");
    if (msg.Type == "testMsg")
    {
        TestMsg? tm = hydraTests.ParseTestMsg(msg.MessageJson ?? "");
        Console.WriteLine($"msg: {tm?.Bdy?.Msg}, id: {tm?.Bdy?.Id}");
        await hydraTests.SendMessage();
    }
    else if (msg.Type == "ping")
    {
        PingMsg? pm = hydraTests.ParsePingMsg(msg.MessageJson ?? "");
        Console.WriteLine($"message: {pm?.Bdy?.Message}");
    }
    await Task.Delay(1);
});

// Initialize Hydra 
await hydra.Init();

// Tests
//hydraTests.CreateUMFMessage();
//hydraTests.TestUMFParseRoutes();
//await hydraTests.GetPresence("hmr-service");
await hydraTests.TestMessagingQueuing();

// Prevent app from closing
await host.RunAsync();

