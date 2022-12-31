using System.Text.Json;

namespace Hydra4NET;

public class QueueProcessor
{
    private readonly Hydra _hydra;
    private PeriodicTimer _timer;
    private CancellationTokenSource _cts = new();

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

    public QueueProcessor(Hydra hydra)
    {
        _hydra = hydra;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds((double)SlidingDuration.BaseDelay));
        Task? temp = StartQueueProcessor(); // allows for calling StartQueueProcessor without awaiting
    }

    protected virtual async Task ProcessMessage(string type, string message)
    {
        await Task.Delay(0);
        throw new NotImplementedException();        
    }

    private async Task StartQueueProcessor()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                Console.WriteLine($"QueueProcessor: {DateTime.Now.ToString()}");

                string message = await _hydra.GetQueueMessage(_hydra.ServiceName ?? "");
                if (message != String.Empty)
                {
                    UMFBase? umf = JsonSerializer.Deserialize<UMFBase>(message, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (umf != null)
                    {
                        await ProcessMessage(umf.Typ, message);
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
        switch (_slidingDuration)
        {
            case SlidingDuration.BaseDelay:
                Console.WriteLine("QueueProcessor: updating from NoDelay to ShortDelay");
                _slidingDuration = SlidingDuration.ShortDelay;
                break;
            case SlidingDuration.ShortDelay:
                Console.WriteLine("QueueProcessor: updating from ShortDelay to LongerDelay");
                _slidingDuration = SlidingDuration.LongerDelay;
                break;
            case SlidingDuration.LongerDelay:
                Console.WriteLine("QueueProcessor: updating from LongerDelay to LongestDelay");
                _slidingDuration = SlidingDuration.LongestDelay;
                break;
        }
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds((double)_slidingDuration));
    }
}
