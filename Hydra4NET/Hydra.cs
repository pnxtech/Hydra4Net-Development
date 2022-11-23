namespace Hydra4NET
{
    public class Hydra
    {
        private Task? _internalTask = null;
        private readonly PeriodicTimer _timer;
        private int _secondsTick = 1;
        private readonly CancellationTokenSource _cts = new();

        public Hydra()
        {
            TimeSpan interval = TimeSpan.FromSeconds(1);
            _timer = new PeriodicTimer(interval);
        }

        public void Init()
        {
            _internalTask = UpdatePresence();
        }

        private async Task UpdatePresence()
        {
            try 
            { 
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    await PresenceEvent();
                    if (_secondsTick++ == 3)
                    {
                        await HealthCheckEvent();
                        _secondsTick = 1;
                    }
                }
            }
            catch (OperationCanceledException) 
            {
            }
        }

        private async Task PresenceEvent()
        {
            Console.WriteLine("Handling Update Presence");
            await Task.Delay(100);
        }

        private async Task HealthCheckEvent()
        {
            Console.WriteLine("Handling Update Health");
            await Task.Delay(100);
        }

        public async Task Shutdown() { 
            if (_internalTask != null)
            {
                _cts.Cancel();
                await _internalTask;
                _cts.Dispose();
            }
        }
    }
}
