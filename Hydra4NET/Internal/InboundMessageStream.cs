using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Hydra4NET.Internal
{
    internal class InboundMessageStream : IInboundMessageStream
    {
        public InboundMessageStream(string mid, int? maxBuffer = null)
        {
            Mid = mid;
            _channel = maxBuffer.HasValue
                ? Channel.CreateBounded<IInboundMessage>(maxBuffer.Value)
                : Channel.CreateUnbounded<IInboundMessage>();
        }

        public string Mid { get; set; }
        private Channel<IInboundMessage> _channel;

        public IAsyncEnumerable<IInboundMessage> EnumerateMessagesAsync(CancellationToken ct = default)
        {
            return _channel.Reader.ReadAllAsync(ct);
        }

        public void MarkComplete()
        {
            _channel.Writer.TryComplete();
        }

        public async ValueTask AddMessage(IInboundMessage msg)
        {
            await _channel.Writer.WriteAsync(msg);
        }

        public void Dispose()
        {
            OnDispose();
            MarkComplete();
        }

        public Action OnDispose { get; set; } = () => { };

    }
}
