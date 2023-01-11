using Hydra4NET;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public sealed class HydraBackgroundService : BackgroundService
    {
        public HydraBackgroundService(IHydraEventsHandler events, IHydra hydra, DefaultQueueProcessor queue, ILogger<HydraBackgroundService> logger)
        {
            _events = events;
            _hydra = hydra;
            _queue = queue;
            _logger = logger;
        }
        protected IHydra _hydra;
        private DefaultQueueProcessor _queue;
        private ILogger<HydraBackgroundService> _logger;
        private IHydraEventsHandler _events;

        //called once at app shutdown
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Hydra shutting down");
            _events.OnShutdown(_hydra);
            //since hydra implements idisposable shutdown() doesnt need to be called, it is called in dispose by the DI middleware
            return base.StopAsync(cancellationToken);
        }

        //called once at app start
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {        
                _hydra.OnMessageHandler((umf, type, msg) => _events.OnMessageReceived(umf, type, msg, _hydra));
                await _events.OnInit(_hydra);
                await _hydra.Init();
                _logger.LogInformation($"Hydra {_hydra.ServiceName} ({_hydra.InstanceID}) listening on {_hydra.ServiceIP}");
                _queue.Init(stoppingToken);
            }
            catch(Exception e)
            {
                //this obviosuly means that hydra init errors cannot be caught
                await _events.OnInitError(_hydra, e);
                throw;
            }

        }

    }
}
