﻿using Hydra4NET;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public sealed class DefaultQueueProcessor : QueueProcessor
    {
        private IServiceProvider _services;

        public DefaultQueueProcessor(IHydra hydra, IServiceProvider services) : base(hydra)
        {
            _services = services;
        }

        protected override async Task ProcessMessage(IReceivedUMF? umf, string type, string message)
        {
            using var scope = _services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IHydraEventsHandler>()
                .OnQueueMessageReceived(umf, type, message, Hydra);
        }
    }
}
