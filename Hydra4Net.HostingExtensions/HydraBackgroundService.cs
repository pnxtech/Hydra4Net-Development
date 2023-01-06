using Hydra4NET;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public sealed class HydraBackgroundService : BackgroundService
    {
        public HydraBackgroundService(IHydraEventsHandler events, IHydra hydra)
        {
            _events = events;
            _hydra = hydra;
        }
        protected IHydra _hydra;
        private IHydraEventsHandler _events;

        //called once at app shutdown
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _events.OnShutdown(_hydra);
            //since hydra implements idisposable shutdown() doesnt need to be called, it is called in dispose by the DI middleware
            return base.StopAsync(cancellationToken);
        }

        //called once at app start
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _events.OnInit(_hydra);
                await _hydra.Init();
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
