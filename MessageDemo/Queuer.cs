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

    protected override async Task ProcessMessage(IInboundMessage msg)
    {
        Console.WriteLine("Queuer: retrieved message from queue");
        if (msg.Type == "queuer" && !string.IsNullOrEmpty(msg.MessageJson) )
        {
            Console.WriteLine($"Queuer: processing queued message from sender");
            IUMF<SharedMessageBody>? sm = msg.ReceivedUMF?.ToUMF<SharedMessageBody>();
            if (sm != null)
            {
                int? Id = sm?.Bdy?.Id ?? 0;
                string? Msg = sm?.Bdy?.Msg ?? string.Empty;
                if (Msg != string.Empty)
                {
                    UMF<SharedMessageBody> sharedMessage = new()
                    {
                        To = "sender-svcs:/",
                        Frm = $"{_hydra.InstanceID}@{_hydra.ServiceName}:/",
                        Typ = "complete",
                        Bdy = new()
                        {
                            Id = Id,
                            Msg = $"Queuer: processed message containing {Msg} with ID of {Id}"
                        }
                    };
                    string json = sharedMessage.Serialize();
                    await _hydra.MarkQueueMessageAsync(msg.MessageJson, true);
                    await _hydra.SendMessageAsync(sharedMessage.To, json);
                    Console.WriteLine($"Queuer: sent completion message back to sender");
                }
            }
        }
    }
}
