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
                await SetCacheItems();
                await ProcessCommandMessage(umf);
                break;
            case "complete":
                await RetrieveCacheItems();
                ProcessCompleteMessage(umf);
                break;
        }
    }

    private async Task ProcessCommandMessage(IReceivedUMF umf)
    {
        IUMF<CommandMessageBody> msg = umf.ToUMF<CommandMessageBody>();
        //CommandMessage? msg = umf.Cast<CommandMessage, CommandMessageBody>();
        if (msg != null)
        {
            switch (msg.Bdy?.Cmd)
            {
                case "start":
                    _logger.LogInformation("Sender: queuing message for Queuer");
                    await QueueMessageForQueuer();
                    break;
                case "start-respond":
                    _logger.LogInformation("Sender: sending message for response");
                    await SendResponseMessage();
                    break;
                case "start-respond-stream":
                    _logger.LogInformation("Sender: sending message for response");
                    await SendResponseStreamMessage();
                    break;
            }
        }
    }

    private void ProcessCompleteMessage(IReceivedUMF umf)
    {
        IUMF<SharedMessageBody> msg = umf.ToUMF<SharedMessageBody>();
        //SharedMessage? msg = SharedMessage.Deserialize<SharedMessage>(message);
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

    private async Task SetCacheItems()
    {
        bool bVal = true;
        await _hydra.SetCacheBool("bool", bVal);
        _logger.LogInformation($"Cached bool value: {bVal}");

        string sVal = "String Value:" + _rand.Next().ToString();
        await _hydra.SetCacheString("string", sVal);
        _logger.LogInformation($"Cached string value: '{sVal}'");

        SharedMessageBody oVal = new SharedMessageBody
        {
            Id = _rand.Next(),
            Msg = "Message: " + _rand.Next().ToString()
        };
        await _hydra.SetCacheJson("json", oVal);
        _logger.LogInformation($"Cached JSON value Id: '{oVal.Id}', Msg: '{oVal.Msg}' ");
    }

    private async Task RetrieveCacheItems()
    {
        bool? bVal = await _hydra.GetCacheBool("bool");
        await _hydra.RemoveCacheItem("bool");
        _logger.LogInformation($"Retrieved cached bool value: {bVal}");

        string? sVal = await _hydra.GetCacheString("string");
        await _hydra.RemoveCacheItem("string");
        _logger.LogInformation($"Retrieved cached string value: '{sVal}'");

        SharedMessageBody? oVal = await _hydra.GetCacheJson<SharedMessageBody>("json");
        await _hydra.RemoveCacheItem("json");
        _logger.LogInformation($"Retrieved cached JSON value Id: '{oVal?.Id}', Msg: '{oVal?.Msg}' ");
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
}
