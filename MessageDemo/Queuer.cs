using Hydra4NET;
using MessageDemo.Models;

namespace MessageDemo;

public class Queuer : QueueProcessor
{
    protected Hydra _hydra;

    public Queuer(Hydra hydra) : base(hydra)
    {
        _hydra = hydra;
    }

    protected override async Task ProcessMessage(string type, string message)
    {
        Console.WriteLine("Queuer: recieved message");
        if (type == "queuer")
        {
            SharedMessage? sm = SharedMessage.Deserialize<SharedMessage>(message);
            if (sm != null)
            {
                int? Id = sm?.Bdy?.Id ?? 0;
                string? Msg = sm?.Bdy?.Msg ?? string.Empty;
                if (Msg != string.Empty) 
                {
                    SharedMessage sharedMessage = new()
                    {
                        To = "sender-svcs:/",
                        Frm = $"{_hydra.InstanceID}@{_hydra.ServiceName}:/",
                        Typ = "sender",
                        Bdy = new()
                        {
                            Id = Id,
                            Msg = $"Queuer: processed {Id} message containing {Msg}"
                        }
                    };
                    string json = sharedMessage.Serialize();
                    await _hydra.SendMessage(sharedMessage.To, json);
                    await _hydra.MarkQueueMessage(message, true);
                }
            }
        }
    }
}
