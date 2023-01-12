using Hydra4NET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

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
        public static IServiceCollection AddHydra(this IServiceCollection services, HydraConfigObject config)
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
        /// <typeparam name="THandler"></typeparam>
        /// <param name="services"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddHydraEventHandler<THandler>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton) where THandler : class, IHydraEventsHandler
        {
            services.Add(new ServiceDescriptor(typeof(IHydraEventsHandler), typeof(THandler), lifetime));
            return services;
        }

        /// <summary>
        /// Add an implementation of the IHydraEventsHandler to DI. Optionally inherit the HydraEventsHandler class.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="services"></param>
        /// <param name="implementationFactory"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddHydraEventHandler<THandler>(this IServiceCollection services, Func<IServiceProvider, THandler> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Singleton) where THandler : class, IHydraEventsHandler
        {
            services.Add(new ServiceDescriptor(typeof(IHydraEventsHandler), implementationFactory, lifetime));
            return services;
        }
    }
}
