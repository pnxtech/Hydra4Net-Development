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
        /// <summary>
        /// Adds required Hydra services to DI
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Add an implementation of the IHydraEventsHandler to DI. Optionally inherit the HydraEventsHandler class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHydraEventHandler<T>(this IServiceCollection services) where T: class, IHydraEventsHandler
            => services.AddSingleton<IHydraEventsHandler, T>();
        /// <summary>
        /// Add an implementation of the IHydraEventsHandler to DI. Optionally inherit the HydraEventsHandler class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHydraEventHandler<T>(this IServiceCollection services, Func<IServiceProvider, T> implementationFactory) where T : class, IHydraEventsHandler
            => services.AddSingleton<IHydraEventsHandler>(implementationFactory);
    }
}
