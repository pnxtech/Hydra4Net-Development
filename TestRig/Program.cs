using Hydra4NET;

Hydra hydra = new Hydra();
hydra.Init();

Console.WriteLine("Press any key to shutdown");
Console.ReadKey();
await hydra.Shutdown();
