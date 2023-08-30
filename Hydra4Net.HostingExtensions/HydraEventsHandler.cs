using Hydra4NET;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public class HydraEventsHandler : IHydraEventsHandler
    {
        protected ILogger<HydraEventsHandler> Logger;

        public HydraEventsHandler(ILogger<HydraEventsHandler> logger)
        {
            Logger = logger;
        }
        public virtual Task OnMessageReceived(IInboundMessage msg, IHydra hydra)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnQueueMessageReceived(IInboundMessage msg, IHydra hydra)
        {
            return Task.CompletedTask;
        }

        public virtual Task BeforeInit(IHydra hydra)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnShutdown(IHydra hydra)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnInitError(IHydra hydra, Exception e)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnDequeueError(IHydra hydra, Exception e)
        {
            Logger.LogError(e, "Hydra dequeue error occurred");
            return Task.CompletedTask;
        }

        public virtual Task OnInternalError(IHydra hydra, Exception e)
        {
            Logger.LogError(e, "Internal Hydra error occurred");
            return Task.CompletedTask;
        }

        public virtual void OnDebugEvent(IHydra hydra, DebugEvent e)
        {
            Logger.LogDebug("Hydra: {0}: {1}", e.Message, string.IsNullOrEmpty(e.UMF) ? "(no UMF)" : e.UMF);
        }

        public virtual Task OnRedisConnectionChange(IHydra hydra, RedisConnectionStatus connectionStatus)
        {
            switch (connectionStatus.Status)
            {
                case ConnectionStatus.Connected:
                    Logger.LogDebug(connectionStatus.Message);
                    break;
                case ConnectionStatus.Disconnected:
                    Logger.LogError(connectionStatus.Exception, connectionStatus.Message);
                    break;
                case ConnectionStatus.Reconnected:
                    Logger.LogInformation(connectionStatus.Exception, connectionStatus.Message);
                    break;
            }
            return Task.CompletedTask;
        }
    }
}