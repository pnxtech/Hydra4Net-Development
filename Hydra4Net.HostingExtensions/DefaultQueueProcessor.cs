using Hydra4NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public sealed class DefaultQueueProcessor : QueueProcessor
    {
        private IHydraEventsHandler _handler;

        public DefaultQueueProcessor(IHydra hydra, IHydraEventsHandler handler) : base(hydra)
        {
            _handler = handler;
        }
        protected override Task ProcessMessage(UMF umf, string type, string message) => _handler.OnQueueMessageReceived(umf, type, message, Hydra);
       
    }
}
