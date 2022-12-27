using System.Net;
using System.Net.Sockets;
using StackExchange.Redis;
using Newtonsoft.Json.Linq;

/**
 MIT License
 Copyright (c) 2022 Carlos Justiniano, and Contributors
 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:
 The above copyright notice and this permission notice shall be included in all
 copies or substantial portions of the Software.
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
*/

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
        public string? ServicePort { get; private set; }
        public string? ServiceType { get; private set; }
        public string? ServiceVersion { get; private set; }
        public string? HostName { get; private set; }
        public int ProcessID { get; private set; }
        public string? Architecture { get; private set; }
        public string? NodeVersion { get; private set; }
        public string? InstanceID { get; private set; }

        private IServer? _server;
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
            ServicePort = config?.Hydra?.ServicePort.ToString() ?? "";
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
                _server = _redis.GetServer($"{config?.Hydra?.Redis?.Host}:{config?.Hydra?.Redis?.Port}");
                _db = _redis.GetDatabase();
                await RegisterService();
            }
            else
            {
                Console.WriteLine("Warning, ConnectionMultiplexer returned false");
            }
        }
        #endregion

        /**
         * SendMessage
         * Sends a message to a service instance
         * TODO: Figure out a way to change the function signature to 
         *       accept a UMF<T> class.  It wasn't clear to me how to
         *       use generics for this purpose.
         */
        public async Task SendMessage(string to, string jsonUMFMessage)
        {
            UMFRouteEntry parsedEntry = UMFBase.ParseRoute(to);
            string instanceId = String.Empty;
            if (parsedEntry.Instance != String.Empty)
            {
                instanceId = parsedEntry.Instance;
            }
            else
            {
                List<PresenceNodeEntry>? entries = await GetPresence(parsedEntry.ServiceName);
                if (entries != null && entries.Count > 0)
                {
                    // Always select first array entry because
                    // GetPresence returns a randomized list
                    instanceId = entries[0].InstanceID ?? "";
                }
            }
            if (instanceId != string.Empty && _redis != null)
            {
                ISubscriber sub = _redis.GetSubscriber();
                await sub.PublishAsync($"{_mc_message_key}:{parsedEntry.ServiceName}:{instanceId}", jsonUMFMessage);
            }
        }

        public async Task SendBroadcastMessage(string to, string jsonUMFMessage)
        {
            UMFRouteEntry parsedEntry = UMFBase.ParseRoute(to);
            if (_redis != null)
            {
                ISubscriber sub = _redis.GetSubscriber();
                await sub.PublishAsync($"{_mc_message_key}:{parsedEntry.ServiceName}", jsonUMFMessage);
            }
        }

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

        /** *********************************
        * [[ INTERNAL AND PRIVATE MEMBERS ]]
        * ***********************************
        */

        private async Task RegisterService()
        {
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
        }
    }
}

