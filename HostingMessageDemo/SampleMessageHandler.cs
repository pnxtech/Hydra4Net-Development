using HostingMessageDemo.Models;
using Hydra4Net.HostingExtensions;
using Hydra4NET;
using Microsoft.Extensions.Logging;

namespace HostingMessageDemo
{
    internal class SampleMessageHandler : HydraEventsHandler
    {
        private string _mode = "";
        private readonly Sender _sender;

        public SampleMessageHandler(ILogger<SampleMessageHandler> logger, Sender sender, IHydra hydra) : base(logger)
        {
            _sender = sender;
            SetValidateMode(hydra);
        }

        private class Modes
        {
            public const string Sender = "sender";
            public const string Queuer = "queuer";
        }

        void SetValidateMode(IHydra hydra)
        {
            _mode = hydra?.ServiceType?.ToLower() ?? "unknown";
            switch (_mode)
            {
                case Modes.Sender:
                case Modes.Queuer:
                    Logger.LogInformation($"Configured as {_mode}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("ServiceType", "Hydra config doesn't specify a valid ServiceType role");
            }
        }

        public override async Task OnMessageReceived(IInboundMessage msg, IHydra hydra)
        {
            try
            {
                Logger.LogInformation($"Received message of type {msg.Type}");
                if (_mode == Modes.Sender && msg.ReceivedUMF != null)
                {
                    await _sender.ProcessMessage(msg.Type, msg.ReceivedUMF);
                }
                else if (_mode == Modes.Queuer)
                {
                    if (msg.Type == "respond")
                    {
                        await HandleRespondType(msg, hydra);
                    }
                    else if (msg.Type == "respond-stream")
                    {
                        await HandleResponseStreamType(msg, hydra);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "OnMessageReceived failed");
            }
        }

        public override async Task OnQueueMessageReceived(IInboundMessage msg, IHydra hydra)
        {
            if (msg.Type != Modes.Queuer)
                return;
            try
            {
                Logger.LogInformation($"Queuer: processing queued message from sender");
                if (msg.Type == "queuer")
                {
                    await HandleQueuerType(msg, hydra);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Queue handler failed");
            }
        }

        private async Task HandleQueuerType(IInboundMessage msg, IHydra hydra)
        {
            IUMF<SharedMessageBody>? sm = msg.ReceivedUMF?.ToUMF<SharedMessageBody>();
            if (sm != null)
            {
                string? Msg = sm?.Bdy?.Msg;
                if (Msg != null)
                {
                    int? Id = sm?.Bdy?.Id;
                    IUMF<SharedMessageBody> sharedMessage = hydra.CreateUMF<SharedMessageBody>("sender-svcs:/", "complete", new()
                    {
                        Id = Id,
                        Msg = $"Queuer: processed message containing {Msg} with ID of {Id}"
                    });
                    string json = sharedMessage.Serialize();
                    Logger.LogInformation($"Queuer: mark message: {msg.MessageJson}");
                    await hydra.MarkQueueMessageAsync(msg.MessageJson ?? "", true);
                    Logger.LogInformation($"Queuer: send json: {json}");
                    await hydra.SendMessageAsync(sharedMessage.To, json);
                    Logger.LogInformation($"Queuer: sent completion message back to sender");
                }
                else
                {
                    Logger.LogWarning("Queue Msg null: {0}", msg.MessageJson);
                }
            }
            else
            {
                Logger.LogError("SharedMessage is null, body: {0}", msg.MessageJson);
            }
        }

        private async Task HandleRespondType(IInboundMessage msg, IHydra hydra)
        {
            IUMF<SharedMessageBody>? sm = msg.ReceivedUMF?.ToUMF<SharedMessageBody>();
            string? Msg = sm?.Bdy?.Msg;
            if (sm != null)
            {
                int? Id = sm?.Bdy?.Id;
                IUMF<SharedMessageBody> sharedMessage = hydra.CreateUMFResponse(sm!, "response", new SharedMessageBody
                {
                    Id = Id,
                    Msg = $"Queuer: sending single response to {Msg} with ID of {Id}"
                });
                await hydra.SendMessageAsync(sharedMessage);
                Logger.LogInformation($"Queuer: sent single response message back to sender");
            }
        }

        private async Task HandleResponseStreamType(IInboundMessage msg, IHydra hydra)
        {
            IUMF<SharedMessageBody>? sm = msg.ReceivedUMF?.ToUMF<SharedMessageBody>();
            string? Msg = sm?.Bdy?.Msg;
            if (sm != null)
            {
                int? Id = sm?.Bdy?.Id;
                //Note that these messages are not guaranteed to be received in order by the Sender
                for (var i = 0; i < 5; i++)
                {
                    IUMF<SharedMessageBody> sharedMessage = hydra.CreateUMFResponse(sm!, "response-stream", new SharedMessageBody()
                    {
                        Id = Id,
                        Msg = $"Queuer: sending response stream {i} to {Msg} with ID of {Id}"
                    });
                    await hydra.SendMessageAsync(sharedMessage);
                    Logger.LogInformation($"Queuer: sent response stream message back to sender");
                }
                IUMF<SharedMessageBody> completeMsg = hydra.CreateUMFResponse(sm!, "response-stream-complete", new SharedMessageBody()
                {
                    Id = Id,
                    Msg = $"Queuer: sending complete response stream to {Msg} with ID of {Id}"
                });
                await hydra.SendMessageAsync(completeMsg);
                Logger.LogInformation($"Queuer: sent response stream complete message back to sender");
            }
        }

        #region Optional
        public override Task BeforeInit(IHydra hydra)
        {
            Logger.LogInformation($"Hydra initialized");
            return base.BeforeInit(hydra);
        }

        public override Task OnShutdown(IHydra hydra)
        {
            Logger.LogInformation($"Hydra shut down");
            return base.OnShutdown(hydra);
        }

        public override Task OnInitError(IHydra hydra, Exception e)
        {
            Logger.LogCritical(e, "A fatal error occurred initializing Hydra");
            return base.OnInitError(hydra, e);
        }

        public override Task OnDequeueError(IHydra hydra, Exception e)
        {
            //base class logs this (Error level) by default
            return base.OnDequeueError(hydra, e);
        }

        public override Task OnInternalError(IHydra hydra, Exception e)
        {
            //base class logs this (Error level) by default
            return base.OnInternalError(hydra, e);
        }

        //base class logs this (Debug level) by default
        public override void OnDebugEvent(IHydra hydra, DebugEvent e)
        {
            //base class logs this (Error level) by default
            base.OnDebugEvent(hydra, e);
        }

        #endregion Optional
    }
}
