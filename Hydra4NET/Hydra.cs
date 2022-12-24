using System.Net;
using System.Net.Sockets;
using StackExchange.Redis;
using Newtonsoft.Json.Linq;

namespace Hydra4NET
{
    /**
     * Hydra is the main class for the Hydra4NET library.
     * It is responsible for initializing the Hydra library and
     * shutting it down.
     */
    public partial class Hydra
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

        #region Class variables 
        private Task? _internalTask = null;
        private readonly PeriodicTimer _timer;
        private int _secondsTick = 1;
        private readonly CancellationTokenSource _cts = new();
        public string? ServiceName { get; private set; }
        public string? ServiceDescription { get; private set; }
        public string? ServiceIP { get; private set; }
        public int ServicePort { get; private set; }
        public string? ServiceType { get; private set; }
        public string? ServiceVersion { get; private set; }
        public string? HostName { get; private set; }
        public int ProcessID { get; private set; }
        public string? Architecture { get; private set; }
        public string? NodeVersion { get; private set; }
        public string? InstanceID { get; private set; }

        private ConnectionMultiplexer? _redis;
        private IDatabase? _db;
        #endregion // Class variables

        #region Message delegate
        public delegate Task MessageHandler(string type, string? message);
        private MessageHandler? _MessageHandler = null;
        #endregion // Message delegate

        public Hydra()
        {
            TimeSpan interval = TimeSpan.FromSeconds(_ONE_SECOND);
            _timer = new PeriodicTimer(interval);
        }

        #region Initialization
        public async Task Init(HydraConfigObject config)
        {
            _internalTask = UpdatePresence();
            HostName = Dns.GetHostName();
            ProcessID = Environment.ProcessId;
            Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            NodeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            ServiceName = config?.Hydra?.ServiceName;
            ServiceDescription = config?.Hydra?.ServiceDescription;
            ServiceType = config?.Hydra?.ServiceType;
            ServicePort = config?.Hydra?.ServicePort ?? 0;
            ServiceIP = config?.Hydra?.ServiceIP;
            if (ServiceIP == null || ServiceIP == String.Empty)
            {
                using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    ServiceIP = endPoint.Address.ToString();
                }
            }

            InstanceID = Guid.NewGuid().ToString();
            InstanceID = InstanceID.Replace("-", "");

            Console.WriteLine($"{ServiceName} ({InstanceID}) listening on {ServiceIP}");

            string connectionString = $"{config?.Hydra?.Redis?.Host}:{config?.Hydra?.Redis?.Port},defaultDatabase={config?.Hydra?.Redis?.Db}";
            if (config?.Hydra?.Redis?.Options != String.Empty)
            {
                connectionString = $"{connectionString},{config?.Hydra?.Redis?.Options}";
            }
            _redis = ConnectionMultiplexer.Connect(connectionString);
            if (_redis != null)
            {
                _db = _redis.GetDatabase();
                await RegisterService();
            }
        }
        #endregion

        public void OnMessageHandler(MessageHandler handler)
        {
            if (handler != null)
            {
                _MessageHandler = handler;
            }
        }

        public void Shutdown()
        {
            if (_internalTask != null)
            {
                _cts.Cancel();
                //_internalTask;
                _cts.Dispose();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////// [[ INTERNAL AND PRIVATE MEMBERS ]] ////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task RegisterService()
        {
            if (_redis != null)
            {
                ISubscriber subChannel1 = _redis.GetSubscriber();
                ISubscriber subChannel2 = _redis.GetSubscriber();
                subChannel1.Subscribe($"{_mc_message_key}:{ServiceName}").OnMessage(async channelMessage => {
                    if (_MessageHandler != null)
                    {
                        string msg = (string?)channelMessage.Message ?? String.Empty;
                        JObject o = JObject.Parse(msg);
                        string type = (string?)o["typ"] ?? String.Empty;
                        await _MessageHandler(type, msg);
                    }
                });
                subChannel2.Subscribe($"{_mc_message_key}:{ServiceName}:{InstanceID}").OnMessage(async channelMessage => {
                    if (_MessageHandler != null)
                    {
                        string msg = (string?)channelMessage.Message ?? String.Empty;
                        JObject o = JObject.Parse(msg);
                        string type = (string?)o["typ"] ?? String.Empty;
                        await _MessageHandler(type, msg);
                    }
                });
            }
            if (_db != null)
            {
                await _db.StringSetAsync($"{_redis_pre_key}:{ServiceName}:service", Serialize(new RegistrationEntry
                {
                    ServiceName = ServiceName,
                    Type = ServiceType,
                    RegisteredOn = GetTimestamp()
                }));
                await _db.KeyExpireAsync($"{_redis_pre_key}:{ServiceName}:service", TimeSpan.FromSeconds(_KEY_EXPIRATION_TTL));
            }
        }
    }
}
