using HostingMessageDemo.Models;
using Hydra4NET;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HostingMessageDemo;

public class Sender
{
    private IHydra _hydra;
    private ILogger<Sender> _logger;

    public Sender(IHydra hydra, ILogger<Sender> logger)
    {
        _hydra = hydra;
        _logger = logger;
    }

    //TODO: demo caching with IDistributedCache? 
    public async Task ProcessMessage(string type, IReceivedUMF umf)
    {
        switch (type) // Messages dispatcher
        {
            case "start":
                await ProcessCommandMessage(umf);
                break;
            case "complete":
                ProcessCompleteMessage(umf);
                break;
        }
    }

    private Task ProcessCommandMessage(IReceivedUMF umf)
    {
        IUMF<CommandMessageBody> msg = umf.ToUMF<CommandMessageBody>();
        if (msg != null)
        {
            switch (msg.Bdy?.Cmd)
            {
                case "start":
                    _logger.LogInformation("Sender: queuing message for Queuer");
                    return QueueMessageForQueuer();
                case "start-respond":
                    _logger.LogInformation("Sender: sending message for response");
                    return SendResponseMessage();
                case "start-respond-stream":
                    _logger.LogInformation("Sender: sending message for stream response");
                    return SendResponseStreamMessage();
                case "start-get-nodes":
                    _logger.LogInformation("Sender: retrieving service nodes");
                    return GetServiceNodes();
            }
        }
        return Task.CompletedTask;
    }

    private void ProcessCompleteMessage(IReceivedUMF umf)
    {
        IUMF<SharedMessageBody> msg = umf.ToUMF<SharedMessageBody>();
        if (msg != null)
        {
            _logger.LogInformation($"Sender: complete message received {msg.Bdy?.Msg}");
        }
    }

    private async Task QueueMessageForQueuer()
    {
        UMF<SharedMessageBody> sharedMessage = new()
        {
            To = "queuer-svcs:/",
            Frm = _hydra.GetServiceFrom(),
            Typ = "queuer",
            Bdy = new()
            {
                Id = _rand.Next(),
                Msg = "Sample job queue message"
            }
        };
        _logger.LogDebug($"Sending message for queuer: {sharedMessage.Serialize()}");
        await _hydra.QueueMessageAsync(sharedMessage);
    }

    static readonly Random _rand = new Random();

    private async Task SendResponseMessage()
    {
        try
        {
            var msg = _hydra.CreateUMF<SharedMessageBody>("queuer-svcs:/", "respond", new()
            {
                Id = _rand.Next(),
                Msg = "Requesting response..."
            });
            IInboundMessage<SharedMessageBody> resp = await _hydra.GetUMFResponseAsync<SharedMessageBody>(msg, "response");
            IUMF<SharedMessageBody>? umf = resp?.ReceivedUMF;
            _logger.LogInformation($"Single response received: {umf?.Bdy?.Msg}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to receive single message");
        }
    }

    private async Task SendResponseStreamMessage()
    {
        var msg = _hydra.CreateUMF<SharedMessageBody>("queuer-svcs:/", "respond-stream", new()
        {
            Id = _rand.Next(),
            Msg = "Requesting response..."
        });
        using (IInboundMessageStream<SharedMessageBody> resp = await _hydra.GetUMFResponseStreamAsync<SharedMessageBody>(msg))
        {
            await foreach (var rMsg in resp.EnumerateMessagesAsync())
            {
                //umfs are cast for you 
                IUMF<SharedMessageBody>? rUmf = rMsg?.ReceivedUMF;
                if (rMsg?.Type == "response-stream")
                    _logger.LogInformation($"Response stream message received: {rUmf?.Bdy?.Msg}");
                else if (rMsg?.Type == "response-stream-complete")
                {
                    _logger.LogInformation("Response stream complete");
                    break;
                }
            }
        }
    }

    private async Task GetServiceNodes()
    {
        var nodes = await _hydra.GetServiceNodesAsync();
        _logger.LogInformation($"Current service nodes: " + JsonSerializer.Serialize(nodes));
    }
}
