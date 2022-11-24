using Hydra4NET;

Config config = new Config();
config.Load("./configs/config.json");
Hydra hydra = new Hydra();
hydra.Init(config);

Console.WriteLine("Press any key to shutdown");
Console.ReadKey();
await hydra.Shutdown();
