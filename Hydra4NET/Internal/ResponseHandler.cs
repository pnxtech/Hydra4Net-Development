using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Hydra4NET.Internal
{
    internal class ResponseHandler
    {
        //Used for preventing multiple registrations per mid
        private ConcurrentDictionary<string, object> _waitingMids
            = new ConcurrentDictionary<string, object>();

        //tracks single responses
        private ConcurrentDictionary<(string mid, string type), TaskCompletionSource<IInboundMessage>> _waitingRepsonses
           = new ConcurrentDictionary<(string, string), TaskCompletionSource<IInboundMessage>>();

        //track stream responses
        private ConcurrentDictionary<string, InboundMessageStream> _waitingStreamResponses
           = new ConcurrentDictionary<string, InboundMessageStream>();

        //create a predictable but unlikely to clash replacement for null values, in case they want to use an empty string as a valid type
        string GetNullType(string mid) => mid.GetHashCode().ToString();

        (string mid, string type) GetKey(string mid, string? type) => (mid, type ?? GetNullType(mid));

        /// <summary>
        /// Ensures that more than one registration of the same mid will not be allowed, throws if already registered
        /// </summary>
        /// <param name="mid"></param>
        /// <exception cref="Exception"></exception>
        void ConfirmMessageNotResgistered(string mid)
        {
            if (_waitingMids.TryGetValue(mid, out _) || _waitingStreamResponses.TryGetValue(mid, out _))
                throw new Exception($"Message {mid} is already waiting for a response");
        }
        public void RegisterResponse(string mid, string? type, TaskCompletionSource<IInboundMessage> tcs)
        {
            ConfirmMessageNotResgistered(mid);
            //what if add fails??
            _waitingMids.TryAdd(mid, new object());
            _waitingRepsonses.TryAdd(GetKey(mid, type), tcs);
        }

        public async ValueTask TryResolveResponses(IInboundMessage msg)
        {
            if (_waitingMids.IsEmpty || string.IsNullOrEmpty(msg.ReceivedUMF?.Rmid))
                return;
            if (!TryResolveResponse(msg))
                await TryResolveStreamResponse(msg);
        }

        //if a large number of incorrectly-typed responses come back before resolution, that is the only way perf could be degraded,
        //although it would only be for those responses and if they came back in quick succession
        //maybe we should only be looking at the first response and throwing if it is not the expected type? this would remove the need for locking
        bool TryResolveResponse(IInboundMessage msg)
        {
            if (_waitingRepsonses.IsEmpty || string.IsNullOrEmpty(msg.ReceivedUMF?.Rmid))
                return false;
            //check still blocked
            if (_waitingMids.TryGetValue(msg.ReceivedUMF.Rmid, out var midLock))
            {
                //make this check atomic. Threads to reach this point are responses to message before type condition is met
                lock (midLock)
                {
                    var umf = msg.ReceivedUMF;
                    //check whether a typed or untyped response has been registered
                    if (_waitingRepsonses.TryRemove(GetKey(umf.Rmid, null), out var tcs)
                        || _waitingRepsonses.TryRemove(GetKey(umf.Rmid, msg.Type), out tcs))
                    {
                        tcs.SetResult(msg);
                        _waitingMids.TryRemove(msg.ReceivedUMF.Rmid, out _);
                        return true;
                    }
                }
            }
            return false;
        }

        async ValueTask<bool> TryResolveStreamResponse(IInboundMessage msg)
        {
            if (_waitingStreamResponses.IsEmpty || string.IsNullOrEmpty(msg.ReceivedUMF?.Rmid))
                return false;
            if (_waitingStreamResponses.TryGetValue(msg.ReceivedUMF.Rmid, out var stream))
            {
                await stream.AddMessage(msg);
                return true;
            }
            return false;
        }

        public void ClearResponse(string mid, string? type)
        {
            _waitingRepsonses.TryRemove(GetKey(mid, type), out _);
            _waitingMids.TryRemove(mid, out _);
        }

        public void ClearStreamResponse(string mid)
        {
            _waitingStreamResponses.TryRemove(mid, out _);
            _waitingMids.TryRemove(mid, out _);
        }

        public InboundMessageStream RegisterResponseStream(string mid)
        {
            ConfirmMessageNotResgistered(mid);
            InboundMessageStream resp = new InboundMessageStream(mid);
            //what if add fails??
            _waitingMids.TryAdd(mid, new object());
            _waitingStreamResponses.TryAdd(mid, resp);
            resp.OnDispose = () => ClearStreamResponse(mid);
            return resp;
        }

        public InboundMessageStream<TResBdy> RegisterResponseStream<TResBdy>(string mid) where TResBdy: new()
        {
            ConfirmMessageNotResgistered(mid);
            InboundMessageStream<TResBdy> resp = new InboundMessageStream<TResBdy>(mid);
            //what if add fails??
            _waitingMids.TryAdd(mid, new object());
            _waitingStreamResponses.TryAdd(mid, resp);
            resp.OnDispose = () => ClearStreamResponse(mid);
            return resp;
        }
    }
}
