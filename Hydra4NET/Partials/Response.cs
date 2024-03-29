﻿using Hydra4NET.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hydra4NET
{
    public partial class Hydra
    {
        //TODO: prevent deserializing twice? once for IReceivedUMF and again to cast in typed methods
        public async Task<IInboundMessage> GetUMFResponseAsync(IUMF umf, string? expectedType = null
           , TimeSpan? timeout = null, CancellationToken ct = default)
        {
            if (umf is null)
                throw new ArgumentNullException(nameof(umf));
            timeout ??= TimeSpan.FromSeconds(30);
            var tcs = new TaskCompletionSource<IInboundMessage>();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            //prevent this task from waiting forever.
            cts.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));
            _responseHandler.RegisterResponse(umf.Mid, expectedType, tcs);
            try
            {
                if (!await SendMessageAsync(umf))
                    throw new HydraException("Unable to send message, no recipients found", HydraException.ErrorType.NoRecipientService);
                return await tcs.Task;
            }
            finally
            {
                _responseHandler.ClearResponse(umf.Mid, expectedType);
            }
        }

        public async Task<IInboundMessage<TResBdy>> GetUMFResponseAsync<TResBdy>(IUMF umf, string expectedType, TimeSpan? timeout = null, CancellationToken ct = default)
            where TResBdy : new()
        {
            if (umf is null)
                throw new ArgumentNullException(nameof(umf));
            var res = await GetUMFResponseAsync(umf, expectedType, timeout, ct);
            return new InboundMessage<TResBdy>
            {
                ReceivedUMF = res.ReceivedUMF?.ToUMF<TResBdy>(),
                MessageJson = res.MessageJson,
                Type = res.Type
            };
        }

        private async Task SendResponseMessage(bool broadCast, IUMF umf, IDisposable stream)
        {
            if (broadCast)
            {
                await SendBroadcastMessageAsync(umf);
            }
            else if (!await SendMessageAsync(umf))
            {
                stream.Dispose();
                throw new HydraException("Unable to send message, no recipients found", HydraException.ErrorType.NoRecipientService);
            }
        }

        public async Task<IInboundMessageStream> GetUMFResponseStreamAsync(IUMF umf, bool broadCast = false)
        {
            if (umf is null)
                throw new ArgumentNullException(nameof(umf));

            var stream = _responseHandler.RegisterResponseStream(umf.Mid);
            await SendResponseMessage(broadCast, umf, stream);
            return stream;
        }

        public async Task<IInboundMessageStream<TResBdy>> GetUMFResponseStreamAsync<TResBdy>(IUMF umf, bool broadCast = false)
            where TResBdy : new()
        {
            if (umf is null)
                throw new ArgumentNullException(nameof(umf));
            var stream = _responseHandler.RegisterResponseStream<TResBdy>(umf.Mid);
            await SendResponseMessage(broadCast, umf, stream);
            return stream;
        }
    }
}
