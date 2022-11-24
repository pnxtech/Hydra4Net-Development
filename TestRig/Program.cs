using Hydra4NET;

Config config = new Config();
Hydra hydra = new Hydra();
hydra.Init(config);

Console.WriteLine("Press any key to shutdown");
Console.ReadKey();
await hydra.Shutdown();
