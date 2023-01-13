using Hydra4NET;
using System;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public abstract class HydraEventsHandler : IHydraEventsHandler
    {
        public abstract Task OnMessageReceived(IReceivedUMF? umf, string type, string? message, IHydra hydra);

        public abstract Task OnQueueMessageReceived(IReceivedUMF? umf, string type, string? message, IHydra hydra);

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
    }
}
