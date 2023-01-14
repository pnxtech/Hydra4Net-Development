using System;
using System.Collections.Generic;
using System.Threading;

namespace Hydra4NET
{
    public interface IInboundMessageStream : IDisposable
    {
        IAsyncEnumerable<IInboundMessage> EnumerateMessagesAsync(CancellationToken ct = default);
        void MarkComplete();
    }
}