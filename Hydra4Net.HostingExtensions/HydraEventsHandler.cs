using Hydra4NET;
using System;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public abstract class HydraEventsHandler : IHydraEventsHandler
    {
        public abstract Task OnMessageReceived(IInboundMessage msg, IHydra hydra);

        public abstract Task OnQueueMessageReceived(IInboundMessage msg, IHydra hydra);

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
            return Task.CompletedTask;
        }
    }
}
