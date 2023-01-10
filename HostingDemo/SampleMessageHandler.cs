using HostingDemo.Models;
using Hydra4Net.HostingExtensions;
using Hydra4NET;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostingDemo
{
    internal class SampleMessageHandler : HydraEventsHandler
    {
        private ILogger<SampleMessageHandler> _logger;

        private string _mode;
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

        public override async Task OnMessageReceived(UMF umf, string type, string? message, IHydra hydra)
        {
            _logger.LogInformation($"Received message of type {type}");
            if (_mode == Modes.Sender && umf != null)
            {
                await _sender.ProcessMessage(type, umf);
                Console.WriteLine();
            }
        }
        public override async Task OnQueueMessageReceived(UMF umf, string type, string? message, IHydra hydra)
        {
            if (type != Modes.Queuer)
                return;
            try
            {
                _logger.LogInformation($"Queuer: processing queued message from sender");
                SharedMessage? sm = umf.Cast<SharedMessage, SharedMessageBody>();
                if (sm != null)
                {
                    int? Id = sm?.Bdy?.Id ?? 0;
                    string? Msg = sm?.Bdy?.Msg ?? string.Empty;
                    if (Msg != string.Empty)
                    {
                        SharedMessage sharedMessage = new()
                        {
                            To = "sender-svcs:/",
                            Frm = $"{hydra.InstanceID}@{hydra.ServiceName}:/",
                            Typ = "complete",
                            Bdy = new()
                            {
                                Id = Id,
                                Msg = $"Queuer: processed message containing {Msg} with ID of {Id}"
                            }
                        };
                        string json = sharedMessage.Serialize();
                        _logger.LogInformation($"Queuer: mark message: {message}");
                        await hydra.MarkQueueMessage(message, true);
                        _logger.LogInformation($"Queuer: send json: {json}");
                        await hydra.SendMessage(sharedMessage.To, json);
                        _logger.LogInformation($"Queuer: sent completion message back to sender");
                    }
                    else
                    {
                        _logger.LogWarning("Queue Msg null: {0}", message);
                    }
                }
                else
                {
                    _logger.LogError("SharedMessage is null, body: {0}", umf.MessageJson);
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Queue handler failed");
            }

        }

        #region Optional
        public override Task OnInit(IHydra hydra)
        {
            _logger.LogInformation($"Hydra initialized");
            return base.OnInit(hydra);
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


        #endregion Optional
    }
}
