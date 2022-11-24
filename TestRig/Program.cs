using Hydra4NET;

Hydra hydra = new Hydra();
Config config = new Config();

config.Load("./configs/config.json");
hydra.Init(config);

Console.WriteLine("Press any key to shutdown");
Console.ReadKey();
await hydra.Shutdown();
