using Hydra4NET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddHydraServices(this IServiceCollection services, HydraConfigObject config)
        {
            services.TryAddSingleton<IHydra>((s =>
            {
                var hydra = new Hydra(config);
                //could do this before init in the background service also
                return hydra;
            }));
            services.AddHostedService<HydraBackgroundService>();
            services.AddSingleton(config); // dangerous since HydraConfigObject is not immutable, but it only matters at init()?
            services.AddSingleton<DefaultQueueProcessor>();
            return services;
        }
        public static IServiceCollection AddHydraEventHandler<T>(this IServiceCollection services) where T: class, IHydraEventsHandler
            => services.AddSingleton<IHydraEventsHandler, T>();  
        
        public static IServiceCollection AddHydraEventHandler<T>(this IServiceCollection services, Func<IServiceProvider, T> implementationFactory) where T : class, IHydraEventsHandler
            => services.AddSingleton<IHydraEventsHandler>(implementationFactory);
    }
}
