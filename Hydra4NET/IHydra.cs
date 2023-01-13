﻿using System;
using System.Collections.Generic;
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
        Task<List<Hydra.PresenceNodeEntry>> GetPresence(string serviceName);

        /// <summary>
        /// Retrieves a message from a service's queue
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        Task<string> GetQueueMessage(string serviceName);

        /// <summary>
        /// Retrieves a message from this Hydra instance service's queue
        /// </summary>
        /// <returns></returns>
        Task<string> GetQueueMessage();

        /// <summary>
        /// Initializes Hydra, accepting an optional config option which will override the one at initialization
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        /// <exception cref="HydraInitException"></exception>
        Task Init(HydraConfigObject? config = null);

        /// <summary>
        /// Message is popped off the "in progress" queue and if the completed flag is set to false then the message is requeued on the the "mqrecieved" queue.
        /// Note at this time this function does not support a reason code for requeuing.
        /// </summary>
        /// <param name="jsonUMFMessage"></param>
        /// <param name="completed"></param>
        /// <returns></returns>
        Task<string> MarkQueueMessage(string jsonUMFMessage, bool completed);
        /// <summary>

        /// Registers the event handler for when a message is received
        /// </summary>
        /// <param name="handler"></param>
        void OnMessageHandler(Hydra.UMFMessageHandler handler);

        /// <summary>
        /// Adds a message to a services queue
        /// </summary>
        /// <param name="jsonUMFMessage"></param>
        /// <returns></returns>
        Task QueueMessage(string jsonUMFMessage);

        /// <summary>
        /// Serializes and adds a message to a services queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="umfHeader"></param>
        /// <returns></returns>
        Task QueueMessage<T>(UMF<T> message) where T : new();

        /// <summary>
        /// Sends a message to all instances of a service
        /// </summary>
        /// <param name="to"></param>
        /// <param name="jsonUMFMessage"></param>
        /// <returns></returns>
        Task SendBroadcastMessage(string to, string jsonUMFMessage);

        /// <summary>
        /// Serializes and sends a message to all instances of a service
        /// </summary>
        /// <param name="to"></param>
        /// <param name="jsonUMFMessage"></param>
        /// <returns></returns>
        Task SendBroadcastMessage<T>(UMF<T> message) where T : new();

        /// <summary>
        /// Sends a message to a service instance
        /// </summary>
        /// <param name="to"></param>
        /// <param name="jsonUMFMessage"></param>
        /// <returns></returns>
        Task SendMessage(string to, string jsonUMFMessage);

        /// <summary>
        /// Serializes and sends a message to a service instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendMessage<T>(UMF<T> message) where T : new();

        /// <summary>
        /// Called by Dispose(). Cleans up resources associated with this instance.
        /// </summary>
        void Shutdown();
    }
}