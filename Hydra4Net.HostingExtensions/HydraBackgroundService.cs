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
            _logger.LogInformation("Hydra shutting down");
            await _hydra.ShutdownAsync(ct: cancellationToken);
            await base.StopAsync(cancellationToken);
        }

        async Task PerformHandlerAction(Func<IHydraEventsHandler, Task> action)
        {
            using var scope = _services.CreateScope();
            await action(scope.ServiceProvider.GetRequiredService<IHydraEventsHandler>());
        }

        void PerformHandlerAction(Action<IHydraEventsHandler> action)
        {
            using var scope = _services.CreateScope();
            action(scope.ServiceProvider.GetRequiredService<IHydraEventsHandler>());
        }

        //called once at app start
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _hydra.OnMessageHandler((msg) => PerformHandlerAction(e => e.OnMessageReceived(msg, _hydra)));
                _hydra.OnInternalErrorHandler((ex) => PerformHandlerAction(e => e.OnInternalError(_hydra, ex)));
                _hydra.OnDebugEventHandler((ev) => PerformHandlerAction(e => e.OnDebugEvent(_hydra, ev)));
                await PerformHandlerAction(e => e.BeforeInit(_hydra));
                await _hydra.InitAsync();
                _queue.Init(stoppingToken);
                _queue.OnDequeueError(e => PerformHandlerAction(h => h.OnDequeueError(_hydra, e)));
                _logger.LogInformation($"Hydra {_hydra.ServiceName} ({_hydra.InstanceID}) connected to {_hydra.GetRedisConfig().GetRedisHost()} and listening on {_hydra.ServiceIP}");
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