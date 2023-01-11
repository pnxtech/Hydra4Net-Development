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
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="umf"></param>
        /// <param name="hydra"></param>
        /// <returns></returns>
        Task OnMessageReceived(UMF? umf, string type, string? message,  IHydra hydra);

        /// <summary>
        /// Called when a new enqued message is received by Hydra
        /// </summary>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="umf"></param>
        /// <param name="hydra"></param>
        /// <returns></returns>
        Task OnQueueMessageReceived(UMF? umf, string type, string? message,  IHydra hydra);
        /// <summary>
        /// Called before Hydra is initialized at application startup
        /// </summary>
        /// <param name="hydra"></param>
        /// <returns></returns>
        Task OnInit(IHydra hydra);
        /// <summary>
        /// Called after a Hydra initialization error occurs, before throwing
        /// </summary>
        /// <param name="hydra"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        Task OnInitError(IHydra hydra, Exception exception);
        /// <summary>
        /// Called on application shutdown
        /// </summary>
        /// <param name="hydra"></param>
        /// <returns></returns>
        Task OnShutdown(IHydra hydra);
    }
}