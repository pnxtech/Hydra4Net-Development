using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        protected Channel<IInboundMessage> _channel;

        private readonly  ThreadSafeBool _isDisposed = false;

        public bool IsDisposed
        {
            get => _isDisposed.Value;
            private set => _isDisposed.Value = value;
        }
   
        public IAsyncEnumerable<IInboundMessage> EnumerateMessagesAsync(CancellationToken ct = default)
        {
            return _channel.Reader.ReadAllAsync(ct);
        }

        public async ValueTask AddMessage(IInboundMessage msg)
        {
            if(!IsDisposed)
                await _channel.Writer.WriteAsync(msg);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            OnDispose();
            _channel.Writer.TryComplete();
        }

        public Action OnDispose { get; set; } = () => { };
    }

    internal class InboundMessageStream<TResBdy> : InboundMessageStream, IInboundMessageStream<TResBdy> where TResBdy: new()
    {
        public InboundMessageStream(string mid, int? maxBuffer = null) : base(mid, maxBuffer) { }

        public new async IAsyncEnumerable<IInboundMessage<TResBdy>> EnumerateMessagesAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach(IInboundMessage msg in _channel.Reader.ReadAllAsync(ct))
            {
                yield return new InboundMessage<TResBdy>
                {
                    ReceivedUMF = msg.ReceivedUMF?.ToUMF<TResBdy>(),
                    MessageJson = msg.MessageJson,
                    Type = msg.Type
                };                 
            }
        }
    }
}
