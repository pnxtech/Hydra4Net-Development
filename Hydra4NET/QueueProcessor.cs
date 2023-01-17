using Hydra4NET.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hydra4NET
{
    public abstract class QueueProcessor : IDisposable
    {
        private readonly IHydra _hydra;

        protected IHydra Hydra => _hydra;

        Timer? _timer;

        /**
         * The BaseDelay has to be a non zero value for two reasons:
         * 1) Zero would cause and invalid TimeSpan duration
         * 2) Zero would be too short for async processing
         */
        protected enum SlidingDuration
        {
            BaseDelay = 10,
            ShortDelay = 1000,
            LongerDelay = 3000,
            LongestDelay = 5000
        }
        private SlidingDuration _slidingDuration = SlidingDuration.BaseDelay;

        public QueueProcessor(IHydra hydra)
        {
            _hydra = hydra;
        }

        protected abstract Task ProcessMessage(IInboundMessage msg);

        Func<Exception, Task>? _errorHandler;

        public void OnDequeueError(Func<Exception, Task> handler)
        {
            _errorHandler = handler;
        }
        public void Init(CancellationToken ct = default)
        {
            //period = 0 means dont automatically restart. we manually start when done
            _timer = new Timer(async (o) =>
            {
                try
                {
                    string message = await _hydra.GetQueueMessageAsync();
                    if (message != string.Empty)
                    {
                        IReceivedUMF? umf = _hydra.DeserializeReceviedUMF(message);
                        if (umf != null)
                        {
                            await ProcessMessage(new InboundMessage
                            {
                                ReceivedUMF = umf,
                                Type = umf?.Typ ?? "",
                                MessageJson = message
                            });
                            _slidingDuration = SlidingDuration.BaseDelay;
                        }
                    }
                    else
                    {
                        CalculateSlidingDuration();
                    }
                }
                catch (Exception e)
                {
                    if (_errorHandler != null)
                    {
                        try
                        {
                            await _errorHandler(e);
                        }
                        catch { }
                    }
                }
                finally
                {
                    _timer?.Change((int)_slidingDuration, 0);
                }
            }, null, 0, 0);
            //when cancelled, it will stop the timer
            ct.Register(() => _timer?.Change(Timeout.Infinite, 0));
        }

        private void CalculateSlidingDuration()
        {
            switch (_slidingDuration)
            {
                case SlidingDuration.BaseDelay:
                    _slidingDuration = SlidingDuration.ShortDelay;
                    break;
                case SlidingDuration.ShortDelay:
                    _slidingDuration = SlidingDuration.LongerDelay;
                    break;
                case SlidingDuration.LongerDelay:
                    _slidingDuration = SlidingDuration.LongestDelay;
                    break;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
