using System.Text.Json;
using StackExchange.Redis;

namespace Hydra4NET
{
    /**
     * Hydra is the main class for the Hydra4NET library.
     * It is responsible for initializing the Hydra library and
     * shutting it down.
     */
    sealed public class Hydra
    {
        #region Private Consts
        private const int _ONE_SECOND = 1;
        private const int _ONE_WEEK_IN_SECONDS = 604800;
        private const int _PRESENCE_UPDATE_INTERVAL = _ONE_SECOND;
        private const int _HEALTH_UPDATE_INTERVAL = _ONE_SECOND * 5;
        private const int _KEY_EXPIRATION_TTL = _ONE_SECOND * 3;
        private const string _redis_pre_key = "hydra:service";
        private const string _mc_message_key = "hydra:service:mc";
        private const string _INFO = "info";
        private const string _DEBUG = "debug";
        private const string _WARN = "warn";
        private const string _ERROR = "error";
        private const string _FATAL = "fatal";
        private const string _TRACE = "trace";
        #endregion

        private Task? _internalTask = null;
        private readonly PeriodicTimer _timer;
        private int _secondsTick = 1;
        private readonly CancellationTokenSource _cts = new();

        public string? ServiceName { get; set; }

        ConnectionMultiplexer? redis;
        IDatabase? db;

        public Hydra()
        {
            TimeSpan interval = TimeSpan.FromSeconds(_ONE_SECOND);
            _timer = new PeriodicTimer(interval);
            UMF uMF = new UMF();
        }

        #region Initialization
        public void Init(HydraConfigObject config)
        {
            _internalTask = UpdatePresence();
            Console.WriteLine($"{config?.Hydra?.ServiceName}");
            ServiceName = config?.Hydra?.ServiceName;
            String connectionString = $"{config?.Hydra?.Redis?.Host}:{config?.Hydra?.Redis?.Port},defaultDatabase={config?.Hydra?.Redis?.Db}";
            if (config?.Hydra?.Redis?.Options != String.Empty)
            {
                connectionString = $"{connectionString},{config?.Hydra?.Redis?.Options}";
            }
            redis = ConnectionMultiplexer.Connect(connectionString);
            if (redis != null)
            {
                db = redis.GetDatabase();
                string? value = db.StringGet($"{_redis_pre_key}:hydra-router:service");
                Console.WriteLine(value?? string.Empty);
            }            
        }
        #endregion

        #region Presence and Health check handling
        private async Task UpdatePresence()
        {
            try 
            { 
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    await PresenceEvent();
                    if (_secondsTick++ == _HEALTH_UPDATE_INTERVAL)
                    {
                        await HealthCheckEvent();
                        _secondsTick = _ONE_SECOND;
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
        #endregion

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
