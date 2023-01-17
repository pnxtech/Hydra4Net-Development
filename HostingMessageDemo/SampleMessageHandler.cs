using HostingDemo.Models;
using Hydra4Net.HostingExtensions;
using Hydra4NET;
using Microsoft.Extensions.Logging;

namespace HostingDemo
{
    internal class SampleMessageHandler : HydraEventsHandler
    {
        private ILogger<SampleMessageHandler> _logger;

        private string _mode = "";
        private Sender _sender;

        public SampleMessageHandler(ILogger<SampleMessageHandler> logger, HydraConfigObject config, Sender sender)
        {
            _logger = logger;
            //add to appsettings.json file or env
            SetValidateMode(config);
            _sender = sender;
        }
        private class Modes
        {
            public const string Sender = "sender";
            public const string Queuer = "queuer";
        }
        void SetValidateMode(HydraConfigObject config)
        {
            _mode = config?.Hydra?.ServiceType?.ToLower() ?? "unknown";
            switch (_mode)
            {
                case Modes.Sender:
                case Modes.Queuer:
                    _logger.LogInformation($"Configured as {_mode}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("ServiceType", "Hydra config doesn't specify a valid ServiceType role");
            }
        }

        public override async Task OnMessageReceived(IInboundMessage msg, IHydra hydra)
        {
            _logger.LogInformation($"Received message of type {msg.Type}");
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

        public override async Task OnQueueMessageReceived(IInboundMessage msg, IHydra hydra)
        {
            if (msg.Type != Modes.Queuer)
                return;
            try
            {
                _logger.LogInformation($"Queuer: processing queued message from sender");
                if (msg.Type == "queuer")
                {
                    await HandleQueuerType(msg, hydra);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Queue handler failed");
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
                    _logger.LogInformation($"Queuer: mark message: {msg.MessageJson}");
                    await hydra.MarkQueueMessageAsync(msg.MessageJson ?? "", true);
                    _logger.LogInformation($"Queuer: send json: {json}");
                    await hydra.SendMessageAsync(sharedMessage.To, json);
                    _logger.LogInformation($"Queuer: sent completion message back to sender");
                }
                else
                {
                    _logger.LogWarning("Queue Msg null: {0}", msg.MessageJson);
                }
            }
            else
            {
                _logger.LogError("SharedMessage is null, body: {0}", msg.MessageJson);
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
                _logger.LogInformation($"Queuer: sent single response message back to sender");
            }

        }
        private async Task HandleResponseStreamType(IInboundMessage msg, IHydra hydra)
        {
            IUMF<SharedMessageBody>? sm = msg.ReceivedUMF?.ToUMF<SharedMessageBody>();
            string? Msg = sm?.Bdy?.Msg;
            if (sm != null)
            {
                int? Id = sm?.Bdy?.Id;
                for (var i = 0; i < 5; i++)
                {
                    IUMF<SharedMessageBody> sharedMessage = hydra.CreateUMFResponse(sm!, "response-stream", new SharedMessageBody()
                    {
                        Id = Id,
                        Msg = $"Queuer: sending response stream {i} to {Msg} with ID of {Id}"
                    });
                    await hydra.SendMessageAsync(sharedMessage);
                    _logger.LogInformation($"Queuer: sent response stream message back to sender");
                }
                IUMF<SharedMessageBody> completeMsg = hydra.CreateUMFResponse(sm!, "response-stream-complete", new SharedMessageBody()
                {
                    Id = Id,
                    Msg = $"Queuer: sending complete response stream to {Msg} with ID of {Id}"
                });
                await hydra.SendMessageAsync(completeMsg);
                _logger.LogInformation($"Queuer: sent response stream complete message back to sender");
            }
        }

        #region Optional
        public override Task BeforeInit(IHydra hydra)
        {
            _logger.LogInformation($"Hydra initialized");
            return base.BeforeInit(hydra);
        }

        public override Task OnShutdown(IHydra hydra)
        {
            _logger.LogInformation($"Hydra shut down");
            return base.OnShutdown(hydra);
        }

        public override Task OnInitError(IHydra hydra, Exception e)
        {
            _logger.LogCritical(e, "A fatal error occurred initializing Hydra");
            return base.OnInitError(hydra, e);
        }

        public override Task OnDequeueError(IHydra hydra, Exception e)
        {
            _logger.LogWarning(e, "An error occurred while dequeueing Hydra");
            return base.OnDequeueError(hydra, e);
        }

        #endregion Optional
    }
}
