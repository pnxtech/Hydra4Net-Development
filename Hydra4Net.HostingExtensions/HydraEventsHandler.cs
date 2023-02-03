using Hydra4NET;
using System;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public class HydraEventsHandler : IHydraEventsHandler
    {
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
            return Task.CompletedTask;
        }
    }
}
