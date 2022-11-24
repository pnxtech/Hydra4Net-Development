using Hydra4NET;

Hydra hydra = new();
hydra.Init(Config.Load("../../../configs/config.json"));

Console.WriteLine("Press any key to shutdown");
Console.ReadKey();
await hydra.Shutdown();
