using Hydra4NET;

namespace Hydra4Net.HostingExtensions
{
    public interface IHydraEventsHandler
    {
        Task OnInit(IHydra hydra);
        Task OnInitError(IHydra hydra, Exception e);
        Task OnMessageReceived(string type, string? message);
        Task OnShutdown(IHydra hydra);
    }
}