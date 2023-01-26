using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hydra4NET
{
    public interface IHydra : IDisposable, IAsyncDisposable
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
        bool IsInitialized { get; }

        /// <summary>
        /// Returns a list of presence entry for the named service.  
        /// If one or more *:presence entries are found in Redis that means
        /// that one or more instances of the service is available. 
        /// For each avaialble*:presence entry this routine grabs a service
        /// directory entry from the hydra:service:nodes hash in Redis and then
        /// builds a list of PresenceNodeEntry entries. The list is then
        /// randomized(shuffled) and returned.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        Task<List<Hydra.PresenceNodeEntry>> GetPresenceAsync(string serviceName);

        /// <summary>
        /// Retrieves a message from a service's queue
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        Task<string> GetQueueMessageAsync(string serviceName);

        /// <summary>
        /// 
        /// Retrieves a message from this Hydra instance service's queue
        /// </summary>
        /// <returns></returns>
        Task<string> GetQueueMessageAsync();

        /// <summary>
        /// Initializes Hydra, accepting an optional config option which will override the one at initialization
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <exception cref="HydraException"></exception>
        Task InitAsync(HydraConfigObject? config = null);

        /// <summary>
        /// Message is popped off the "in progress" queue and if the completed flag is set to false then the message is requeued on the the "mqrecieved" queue.
        /// Note at this time this function does not support a reason code for requeuing.
        /// </summary>
        /// <param name="jsonUMFMessage"></param>
        /// <param name="completed"></param>
        /// <returns></returns>
        Task<string> MarkQueueMessageAsync(string jsonUMFMessage, bool completed);

        /// <summary>
        /// Registers the asynchronous event handler for when a message is received.
        /// </summary>
        /// <param name="handler"></param>
        void OnMessageHandler(Hydra.UMFMessageHandler handler);

        /// <summary>
        /// Adds a message to a services queue
        /// </summary>
        /// <param name="jsonUMFMessage"></param>
        /// <returns></returns>
        Task QueueMessageAsync(string jsonUMFMessage);

        /// <summary>
        /// Serializes and adds a message to a services queue
        /// </summary>
        /// <param name="umfHeader"></param>
        /// <returns></returns>
        Task QueueMessageAsync(IUMF message);

        /// <summary>
        /// Sends a message to all instances of a service
        /// </summary>
        /// <param name="to"></param>
        /// <param name="jsonUMFMessage"></param>
        /// <returns></returns>
        Task SendBroadcastMessageAsync(string to, string jsonUMFMessage);

        /// <summary>
        /// Serializes and sends a message to all instances of a service
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendBroadcastMessageAsync(IUMF message);

        /// <summary>
        /// Sends a message to a service instance
        /// </summary>
        /// <param name="to"></param>
        /// <param name="jsonUMFMessage"></param>
        /// <returns></returns>
        Task<bool> SendMessageAsync(string to, string jsonUMFMessage);

        /// <summary>
        /// Serializes and sends a message to a service instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<bool> SendMessageAsync(IUMF message);

        /// <summary>
        /// Gets a UMF instance with default values set for sending
        /// </summary>
        /// <typeparam name="TBdy"></typeparam>
        /// <param name="to"></param>
        /// <param name="type"></param>
        /// <param name="bdy"></param>
        /// <param name="rmid"></param>
        /// <returns></returns>
        IUMF<TBdy> CreateUMF<TBdy>(string to, string type, TBdy bdy, string? rmid = null) where TBdy : new();

        /// <summary>
        /// Creates a UMF instance with default values set for sending a response to another UMF
        /// </summary>
        /// <typeparam name="TFromBdy"></typeparam>
        /// <typeparam name="TToBdy"></typeparam>
        /// <param name="umf"></param>
        /// <param name="type"></param>
        /// <param name="bdy"></param>
        /// <returns></returns>
        IUMF<TToBdy> CreateUMFResponse<TToBdy>(IUMF umf, string type, TToBdy bdy) where TToBdy : new();

        /// <summary>
        /// Gets the from route for this Hydra service
        /// </summary>
        /// <returns></returns>
        string GetServiceFrom();

        /// <summary>
        /// Sends a message and gets a response from the first message to respond (via Rmid) with the optional exected type.  The default timeout is 30 seconds.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="expectedType"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IInboundMessage> GetUMFResponseAsync(IUMF msg, string? expectedType = null
            , TimeSpan? timeout = null, CancellationToken ct = default);

        /// <summary>
        /// Sends a message and gets a typed response from the first message to respond (via Rmid) with the optional exected type.  The default timeout is 30 seconds.
        /// </summary>
        /// <typeparam name="TResBdy"></typeparam>
        /// <param name="umf"></param>
        /// <param name="expectedType"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IInboundMessage<TResBdy>> GetUMFResponseAsync<TResBdy>(IUMF umf, string expectedType
            , TimeSpan? timeout = null, CancellationToken ct = default)
            where TResBdy : new();

        /// <summary>
        /// Gets a stream of UMF responses (via Rmid). The stream should be disposed when reading is complete. 
        /// </summary>
        /// <param name="umf"></param>
        /// <param name="broadcast"></param>
        /// <returns></returns>
        Task<IInboundMessageStream> GetUMFResponseStreamAsync(IUMF umf, bool broadcast = false);

        /// <summary>
        /// Gets a stream of typed UMF responses (via Rmid). The stream should be disposed when reading is complete. 
        /// </summary>
        /// <param name="umf"></param>
        /// <param name="broadcast"></param>
        /// <returns></returns>
        Task<IInboundMessageStream<TResBdy>> GetUMFResponseStreamAsync<TResBdy>(IUMF umf, bool broadcast = false) where TResBdy : new();

        /// <summary>
        /// Deserializes a json string into an IReceivedUMF
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        IReceivedUMF? DeserializeReceviedUMF(string json);

        /// <summary>
        /// A ValueTask that resolves when Hydra has been initialized.
        /// </summary>
        /// <returns></returns>
        ValueTask WaitInitialized();

        /// <summary>
        /// Gets Hydra's redis ConnectionMultiplexer instance.  It is strongly recommended to use a different Database number than the one used for Hydra for non-Hydra activity.
        /// </summary>
        /// <returns></returns>
        IConnectionMultiplexer GetRedisConnection();

        /// <summary>
        /// Gets Hydra's redis ConnectionMultiplexer instance once Hydra has been initialized.  Useful if threads other than initializer need the connection.
        /// </summary>
        /// <returns></returns>
        ValueTask<IConnectionMultiplexer> GetRedisConnectionAsync();

        /// <summary>
        /// Called by Dispose(). Cleans up resources associated with this instance.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Called by DisposeAsync().  Cleans up Hydra resources. CancellationToken will cancel the flushing of incomplete message handler actions.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask ShutdownAsync(bool waitForFlush = true, CancellationToken ct = default);
    }
}