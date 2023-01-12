﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hydra4NET
{
    public abstract class QueueProcessor : IDisposable
    {
        private readonly IHydra _hydra;

        protected IHydra Hydra => _hydra;

        Timer _timer;

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

        protected abstract Task ProcessMessage(IReceivedUMF? umf, string type, string message);

        public void Init(CancellationToken ct = default)
        {
            //period = 0 means dont automatically restart. we manually start when done
            _timer = new Timer(async (o) =>
            {
                string message = await _hydra.GetQueueMessage();
                if (message != String.Empty)
                {
                    IReceivedUMF? umf = ReceivedUMF.Deserialize(message);
                    if (umf != null)
                    {
                        await ProcessMessage(umf, umf.Typ, message);
                        _slidingDuration = SlidingDuration.BaseDelay;
                    }
                }
                else
                {
                    CalculateSlidingDuration();
                }
                _timer.Change((int)_slidingDuration, 0);
            }, null, 0, 0);
            //when cancelled, it will stop the timer
            ct.Register(() => _timer.Change(Timeout.Infinite, 0));
        }

        private void CalculateSlidingDuration()
        {
            switch (_slidingDuration)
            {
                case SlidingDuration.BaseDelay:
                    //Console.WriteLine("QueueProcessor: updating from NoDelay to ShortDelay");
                    _slidingDuration = SlidingDuration.ShortDelay;
                    break;
                case SlidingDuration.ShortDelay:
                    //Console.WriteLine("QueueProcessor: updating from ShortDelay to LongerDelay");
                    _slidingDuration = SlidingDuration.LongerDelay;
                    break;
                case SlidingDuration.LongerDelay:
                    //Console.WriteLine("QueueProcessor: updating from LongerDelay to LongestDelay");
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
