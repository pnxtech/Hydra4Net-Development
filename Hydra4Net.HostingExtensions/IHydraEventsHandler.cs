using Hydra4NET;
using System;
using System.Threading.Tasks;

namespace Hydra4Net.HostingExtensions
{
    public interface IHydraEventsHandler
    {
        /// <summary>
        /// Called when a new message is received by Hydra
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="hydra"></param>
        /// <returns></returns>
        Task OnMessageReceived(IInboundMessage msg, IHydra hydra);

        /// <summary>
        /// Called when a new enqued message is received by Hydra
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="hydra"></param>
        /// <returns></returns>
        Task OnQueueMessageReceived(IInboundMessage msg, IHydra hydra);

        /// <summary>
        /// Called before Hydra is initialized at application startup
        /// </summary>
        /// <param name="hydra"></param>
        /// <returns></returns>
        Task BeforeInit(IHydra hydra);

        /// <summary>
        /// Called after a Hydra initialization error occurs, before throwing
        /// </summary>
        /// <param name="hydra"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        Task OnInitError(IHydra hydra, Exception exception);

        /// <summary>
        /// Called after a Hydra dequeueing error occurs
        /// </summary>
        /// <param name="hydra"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        Task OnDequeueError(IHydra hydra, Exception e);

        /// <summary>
        /// Called on application shutdown
        /// </summary>
        /// <param name="hydra"></param>
        /// <returns></returns>
        Task OnShutdown(IHydra hydra);
    }
}