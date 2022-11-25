using Hydra4NET;
using HydraTests;

Hydra hydra = new Hydra();
Tests hydraTests = new(ref hydra);

hydra.Init(Config.Load("../../../configs/config.json"));

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

