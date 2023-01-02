using Hydra4NET;
using MessageDemo.Models;

namespace MessageDemo;

public class Sender
{
    private Hydra _hydra;

    public Sender(Hydra hydra)
    {
        _hydra = hydra;
    }

    public async Task ProcessMessage(string type, string message)
    {            
        switch (type) // Messages dispatcher
        {
            case "start":
                await ProcessCommandMessage(message);
                break;
            case "complete":
                ProcessSenderMessage(message);
                break;
        }
    }

    private async Task ProcessCommandMessage(string message)
    {
        CommandMessage? msg = CommandMessage.Deserialize<CommandMessage>(message);
        if (msg != null)
        {
            switch (msg.Bdy?.Cmd)
            {
                case "start":
                    Console.WriteLine("Sender: queuing message for Queuer");
                    await QueueMessageForQueuer();
                    break;
            }
        }
    }

    private void ProcessSenderMessage(string message)
    {
        SharedMessage? msg = SharedMessage.Deserialize<SharedMessage>(message);
        if (msg != null)
        {
            Console.WriteLine($"Sender: message received {msg.Bdy?.Msg}");
        }
    }

    private async Task QueueMessageForQueuer()
    {
        SharedMessage sharedMessage = new()
        {
            To = "queuer-svcs:/",
            Frm = $"{_hydra.InstanceID}@{_hydra.ServiceName}:/",
            Typ = "queuer",
            Bdy = new()
            {
                Id = 1,
                Msg = "Sample job queue message"
            }
        };
        string json = sharedMessage.Serialize();
        await _hydra.QueueMessage(json);
    }
}
