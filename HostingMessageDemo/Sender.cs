﻿using HostingDemo.Models;
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
        await _hydra.QueueMessage(sharedMessage);
    }
    Random _rand = new Random();
    private async Task SendResponseMessage()
    {
        try
        {
            var msg = _hydra.CreateUMF<SharedMessageBody>("queuer-svcs:/", "respond", new()
            {
                Id = _rand.Next(),
                Msg = "Requesting response..."
            });
            IInboundMessage resp = await _hydra.GetUMFResponse(msg, "response");
            IUMF<SharedMessageBody>? umf = resp?.ReceivedUMF?.ToUMF<SharedMessageBody>();
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
        using (IInboundMessageStream resp = await _hydra.GetUMFResponseStream(msg))
        {
            await foreach (var rMsg in resp.EnumerateMessagesAsync())
            {
                var rUmf = rMsg.ReceivedUMF?.ToUMF<SharedMessageBody>();
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
