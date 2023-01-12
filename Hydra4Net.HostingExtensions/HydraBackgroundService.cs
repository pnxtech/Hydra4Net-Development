using Hydra4NET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public sealed class HydraBackgroundService : BackgroundService
    {
        public HydraBackgroundService(IServiceProvider services, IHydra hydra, DefaultQueueProcessor queue, ILogger<HydraBackgroundService> logger)
        {
            _services = services;
            _hydra = hydra;
            _queue = queue;
            _logger = logger;
        }

        private IServiceProvider _services;
        private IHydra _hydra;
        private DefaultQueueProcessor _queue;
        private ILogger<HydraBackgroundService> _logger;

        //called once at app shutdown
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Hydra shutting down");
            //since hydra implements idisposable shutdown() doesnt need to be called, it is called in dispose by the DI middleware

        }

        async Task PerformHandlerAction(Func<IHydraEventsHandler, Task> action)
        {
            using var scope = _services.CreateScope();
            await action(scope.ServiceProvider.GetRequiredService<IHydraEventsHandler>());
        }

        //called once at app start
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {

                _hydra.OnMessageHandler((umf, type, msg)
                    => PerformHandlerAction(e => e.OnMessageReceived(umf, type, msg, _hydra)));
                await PerformHandlerAction(e => e.BeforeInit(_hydra));
                await _hydra.Init();
                _queue.Init(stoppingToken);
                _logger.LogInformation($"Hydra {_hydra.ServiceName} ({_hydra.InstanceID}) listening on {_hydra.ServiceIP}");
            }
            catch (Exception e)
            {
                //this obviosuly means that hydra init errors cannot be caught
                await PerformHandlerAction(ev => ev.OnInitError(_hydra, e));
                throw;
            }
        }
    }
}
