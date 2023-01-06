using Hydra4Net.HostingExtensions;
using Hydra4NET;
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

        public SampleMessageHandler(ILogger<SampleMessageHandler> logger)
        {
            _logger = logger;
        }

        public override Task OnMessageReceived(string type, string? message)
        {
            _logger.LogInformation($"Type: {type}, message: {message}");
            return Task.CompletedTask;
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
