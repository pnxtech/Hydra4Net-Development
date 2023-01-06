namespace Hydra4NET
{
    public interface IHydra
    {
        string? Architecture { get; }
        string? HostName { get; }
        string? InstanceID { get; }
        string? NodeVersion { get; }
        int ProcessID { get; }
        string? ServiceDescription { get; }
        string? ServiceIP { get; }
        string? ServiceName { get; }
        string? ServicePort { get; }
        string? ServiceType { get; }
        string? ServiceVersion { get; }

        void Dispose();
        Task<List<Hydra.PresenceNodeEntry>> GetPresence(string serviceName);
        Task<string> GetQueueMessage(string serviceName);
        Task Init(HydraConfigObject config = null);
        Task<string> MarkQueueMessage(string jsonUMFMessage, bool completed);
        void OnMessageHandler(Hydra.MessageHandler handler);
        Task QueueMessage(string jsonUMFMessage);
        Task SendBroadcastMessage(string to, string jsonUMFMessage);
        Task SendMessage(string to, string jsonUMFMessage);
        Task SendMessage<T>(UMF<T> message) where T : class, new();
        void Shutdown();
    }
}