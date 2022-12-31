using Hydra4NET;

namespace MessageDemo;

public class Queuer : QueueProcessor
{
    public Queuer(Hydra hydra) : base(hydra)
    {

    }

    protected override async Task ProcessMessage(string type, string message)
    {
        Console.WriteLine($"Queuer: recieved message of {type}: {message}");
        await Task.Delay(1);
    }
}
