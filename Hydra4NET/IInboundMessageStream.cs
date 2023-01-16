using System;
using System.Collections.Generic;
using System.Threading;

namespace Hydra4NET
{
    public interface IInboundMessageStream : IDisposable
    {
        /// <summary>
        /// Enumerates incoming messages which have an Rmid matching the Mid of the sender. Messages will continue to be received until Dispose() is called.  Not to be confused with Redis Streams.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<IInboundMessage> EnumerateMessagesAsync(CancellationToken ct = default);
    }

    public interface IInboundMessageStream<TResBdy> : IInboundMessageStream
    {
        new IAsyncEnumerable<IInboundMessage<TResBdy>> EnumerateMessagesAsync(CancellationToken ct = default);
    }
}