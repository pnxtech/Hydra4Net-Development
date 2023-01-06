using Hydra4NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public abstract class HydraEventsHandler : IHydraEventsHandler
    {
        public abstract Task OnMessageReceived(string type, string? message);
        public virtual Task OnInit(IHydra hydra)
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
