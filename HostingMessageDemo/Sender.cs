using HostingDemo.Models;
using Hydra4NET;
using Microsoft.Extensions.Logging;

namespace HostingDemo;

public class Sender
{
    private IHydra _hydra;
    private ILogger<Sender> _logger;

    public Sender(IHydra hydra, ILogger<Sender> logger)
    {
        _hydra = hydra;
        _logger = logger;
    }

    public async Task ProcessMessage(string type, IReceivedUMF umf)
    {
        switch (type) // Messages dispatcher
        {
            case "start":
                await ProcessCommandMessage(umf);
                break;
            case "complete":
                ProcessSenderMessage(umf);
                break;
        }
    }

    private async Task ProcessCommandMessage(IReceivedUMF umf)
    {
        UMF<CommandMessageBody> msg = umf.ToUMF<CommandMessageBody>();
        //CommandMessage? msg = umf.Cast<CommandMessage, CommandMessageBody>();
        if (msg != null)
        {
            switch (msg.Bdy?.Cmd)
            {
                case "start":
                    _logger.LogInformation("Sender: queuing message for Queuer");
                    await QueueMessageForQueuer();
                    break;
            }
        }
    }

    private void ProcessSenderMessage(IReceivedUMF umf)
    {
        UMF<SharedMessageBody> msg = umf.ToUMF<SharedMessageBody>();
        //SharedMessage? msg = SharedMessage.Deserialize<SharedMessage>(message);
        if (msg != null)
        {
            _logger.LogInformation($"Sender: message received {msg.Bdy?.Msg}");
        }
    }

    private async Task QueueMessageForQueuer()
    {
        UMF<SharedMessageBody> sharedMessage = new()
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
        _logger.LogDebug($"Sending message for queuer: {sharedMessage.Serialize()}");
        await _hydra.QueueMessage(sharedMessage);
    }
}
