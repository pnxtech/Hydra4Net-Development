using System.Text.Json;
namespace Hydra4NET;

public abstract class QueueProcessor : IDisposable
{
    private readonly IHydra _hydra;
    protected IHydra Hydra => _hydra;
    private PeriodicTimer _timer;
    private CancellationTokenSource _cts = new();
    Timer _tTimer;


    /**
     * The BaseDelay has to be a non zero value for two reasons:
     * 1) Zero would cause and invalid TimeSpan duration
     * 2) Zero would be too short for async processing
     */
    private enum SlidingDuration
    {
        BaseDelay = 10,
        ShortDelay = 1000,
        LongerDelay = 3000,
        LongestDelay = 5000
    }
    private SlidingDuration _slidingDuration = SlidingDuration.BaseDelay;

    public  QueueProcessor(IHydra hydra)
    {
        _hydra = hydra;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds((double)SlidingDuration.BaseDelay));
        //Task? temp = StartQueueProcessor(); // allows for calling StartQueueProcessor without awaiting
    }
    //i added the UMF object for easier casting, this breaks the current api though
    protected abstract Task ProcessMessage(UMF? umf, string type, string message);
    //{
    //    await Task.Delay(0);
    //    throw new NotImplementedException();        
    //}
    public void Init(CancellationToken ct = default)
    {
        //period = 0 means dont automatically restart. we manually start when done
        _tTimer = new Timer(async (o) =>
        {
            string message = await _hydra.GetQueueMessage(_hydra.ServiceName ?? "");
            if (message != String.Empty)
            {
                UMF? umf = UMF.Deserialize(message);
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
            _tTimer.Change((int)_slidingDuration, 0);
        }, null, 0, 0);
        //when cancelled, it will stop the timer
        ct.Register(() => _tTimer.Change(Timeout.Infinite, 0));
    }
    private async Task StartQueueProcessor()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                //Console.WriteLine($"QueueProcessor: {DateTime.Now.ToString()}");

                string message = await _hydra.GetQueueMessage(_hydra.ServiceName ?? "");
                if (message != String.Empty)
                {
                    UMF? umf = UMF.Deserialize(message);
                    if (umf != null)
                    {
                        await ProcessMessage(umf, umf.Typ, message);
                        ResetSlidingDuration();
                    }
                }
                else
                {
                    UpdateSlidingDuration();
                }

            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ResetSlidingDuration()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        _slidingDuration = SlidingDuration.BaseDelay;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds((double)_slidingDuration));
    }

    private void UpdateSlidingDuration()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        CalculateSlidingDuration();
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds((double)_slidingDuration));
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
        _tTimer?.Dispose();
        _cts?.Dispose();
        _timer?.Dispose();
    }
}
