using Hydra4NET.Helpers;
using Hydra4NET.Internal;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/*
 MIT License
 Copyright (c) 2022 Carlos Justiniano and contributors
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
    public partial class Hydra : IHydra
    {
        #region Private Consts
        private const int _ONE_SECOND = 1;
        private const int _ONE_WEEK_IN_SECONDS = 604800;
        private const int _PRESENCE_UPDATE_INTERVAL = _ONE_SECOND;
        private const int _HEALTH_UPDATE_INTERVAL = _ONE_SECOND * 5;
        private const int _KEY_EXPIRATION_TTL = _ONE_SECOND * 3;
        private const string _redis_pre_key = "hydra:service";
        private const string _mc_message_key = "hydra:service:mc";
        private const string _nodes_hash_key = "hydra:service:nodes";
        private const string _INFO = "info";
        private const string _DEBUG = "debug";
        private const string _WARN = "warn";
        private const string _ERROR = "error";
        private const string _FATAL = "fatal";
        private const string _TRACE = "trace";
        #endregion

        #region Class variables 
        private Task? _presenceTask = null;
        //private readonly PeriodicTimer _timer;
        private int _secondsTick = 1;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
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

        private readonly ThreadSafeBool _isInitialized = false;
        public bool IsInitialized
        {
            get => _isInitialized.Value;
            private set => _isInitialized.Value = value;
        }

        private IConnectionMultiplexer? _redis;

        #endregion // Class variables

        HydraConfigObject? _config;
        #region Message delegate
        public delegate Task UMFMessageHandler(IInboundMessage msg);
        private UMFMessageHandler? _MessageHandler = null;


        #endregion // Message delegate

        public Hydra(HydraConfigObject config)
        {
            LoadConfig(config);
        }

        void LoadConfig(HydraConfigObject config)
        {
            if (config is null)
                throw new ArgumentNullException("config", "HydraConfigObject cannot be null");
            _config = config;
            ServiceName = _config.ServiceName;
            ServiceDescription = _config.ServiceDescription;
            ServiceType = _config.ServiceType;
            ServicePort = _config.ServicePort.ToString() ?? "";
            ServiceIP = _config.ServiceIP;
        }

        /// <summary>
        /// Retrieves a database instance using the configured DB number
        /// </summary>
        /// <returns></returns>
        /// <exception cref="HydraException"></exception>
        private IDatabase GetDatabase()
        {
            if (_redis == null || _config == null)
                throw new HydraException("Hydra has not been initialized, cannot retrieve a Database instance", HydraException.ErrorType.NotInitialized);
            return _redis.GetDatabase(GetRedisConfig().Db);
        }

        #region Initialization

        public IServer GetServer()
        {
            if (_redis == null)
                throw new HydraException("Hydra has not been initialized, cannot retrieve a Server instance", HydraException.ErrorType.NotInitialized);
            return _redis.GetServer(GetRedisConfig().GetRedisHost());
        }

        public async Task InitAsync(HydraConfigObject? config = null)
        {
            if (IsInitialized)
                throw new HydraException("This instance has already been initialized", HydraException.ErrorType.InitializationError);
            if (config != null)
                LoadConfig(config);
            if (_config is null)
                throw new HydraException("No HydraConfigObject has been provided", HydraException.ErrorType.InitializationError);
            try
            {
                //probably throw if no config passed or invalid?
                HostName = Dns.GetHostName();
                ProcessID = Process.GetCurrentProcess().Id;
                Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                NodeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

                if (ServiceIP == null || ServiceIP == string.Empty)
                {
                    using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                    socket.Connect("8.8.8.8", 65530);
                    if (socket.LocalEndPoint is IPEndPoint endPoint)
                    {
                        ServiceIP = endPoint.Address.ToString();
                    }
                }
                else if (ServiceIP.Contains(".") && ServiceIP.Contains("*"))
                {
                    // then IP address field specifies a pattern match
                    int starPoint = ServiceIP.IndexOf("*");
                    string pattern = ServiceIP.Substring(0, starPoint);
                    string selectedIP = string.Empty;
                    var myhost = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ipaddr in myhost.AddressList)
                    {
                        if (ipaddr.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ip = ipaddr.ToString();
                            if (ip.StartsWith(pattern))
                            {
                                selectedIP = ip;
                                break;
                            }
                        }
                    }
                    ServiceIP = selectedIP;
                }
                InstanceID = Guid.NewGuid().ToString().Replace("-", "");
                _redis = ConnectionMultiplexer.Connect(_config.GetRedisConnectionString());

                //TODO: validate conn string here and give detailed errors if something missing?
                if (_redis != null && _redis.IsConnected)
                {
                    await RegisterService();
                    ConfigurePresenceTask();
                    ConfigureEventsChannel();
                    SetInitializedTrue();
                }
                else
                {
                    throw new HydraException("Failed to initialize Hydra, connection to redis failed", HydraException.ErrorType.InitializationError);
                }
            }
            catch (HydraException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new HydraException("Failed to initialize Hydra", e, HydraException.ErrorType.InitializationError);
            }
        }

        private void SetInitializedTrue()
        {
            IsInitialized = true;
            _initTcs.SetResult(true);
        }

        #endregion

        public Task<bool> SendMessageAsync(string to, string jsonUMFMessage)
            => SendMessage(UMFBase.ParseRoute(to), jsonUMFMessage);

        public Task<bool> SendMessageAsync(IUMF message)
            => SendMessage(message.GetRouteEntry(), message.Serialize());

        private async Task<bool> SendMessage(UMFRouteEntry parsedEntry, string jsonUMFMessage)
        {
            string instanceId = string.Empty;
            if (parsedEntry.Instance != string.Empty)
            {
                instanceId = parsedEntry.Instance;
            }
            else
            {
                PresenceNodeEntryCollection entries = await GetPresenceAsync(parsedEntry.ServiceName);
                if (entries != null && entries.Count > 0)
                {
                    // Pick random presence entry
                    instanceId = entries.GetRandomEntry()?.InstanceID ?? "";
                }
                else
                    EmitDebug(DebugEventType.SendMessage, jsonUMFMessage, "Could not find presence entry to send message");
            }
            if (instanceId != string.Empty && _redis != null)
            {
                EmitDebug(DebugEventType.SendMessage, jsonUMFMessage, $"Sending message to instance {instanceId}");
                await _redis.GetSubscriber().PublishAsync($"{_mc_message_key}:{parsedEntry.ServiceName}:{instanceId}", jsonUMFMessage);
                return true; //TODO: if above returns > 0 and the redis instance is not a cluster, that indicates that message was sent.  Can we safely use this info?
            }
            return false;
        }

        public Task SendBroadcastMessageAsync(IUMF message)
            => SendBroadcastMessage(message.GetRouteEntry(), message.Serialize());

        public Task SendBroadcastMessageAsync(string to, string jsonUMFMessage)
            => SendBroadcastMessage(UMFBase.ParseRoute(to), jsonUMFMessage);

        private async Task SendBroadcastMessage(UMFRouteEntry parsedEntry, string jsonUMFMessage)
        {
            if (_redis != null)
            {
                EmitDebug(DebugEventType.SendBroadcastMessage, jsonUMFMessage, $"Sending broadcast message");
                await _redis.GetSubscriber().PublishAsync($"{_mc_message_key}:{parsedEntry.ServiceName}", jsonUMFMessage);
            }
        }

        public void OnMessageHandler(UMFMessageHandler handler)
        {
            if (handler is null)
                throw new ArgumentNullException("handler", "UMFMessageHandler cannot be null");
            _MessageHandler = handler;
        }

        private async Task QueueMessage(UMFRouteEntry? entry, string jsonUMFMessage)
        {
            if (string.IsNullOrEmpty(entry?.Error))
            {
                EmitDebug(DebugEventType.SendBroadcastMessage, jsonUMFMessage, $"Sending queued message");
                await GetDatabase().ListLeftPushAsync($"{_redis_pre_key}:{entry!.ServiceName}:mqrecieved", jsonUMFMessage);
            }
            else
            {
                EmitDebug(DebugEventType.SendQueue, jsonUMFMessage, $"Route Error queueing message: {entry?.Error}");
            }
        }

        public Task QueueMessageAsync(IUMF umfHeader) =>
            QueueMessage(umfHeader?.GetRouteEntry(), umfHeader?.Serialize() ?? "");

        public Task QueueMessageAsync(string jsonUMFMessage)
        {
            IReceivedUMF? umfHeader = DeserializeReceviedUMF(jsonUMFMessage);
            return QueueMessage(umfHeader?.GetRouteEntry(), jsonUMFMessage);
        }

        //think about deserializing for them
        public async Task<string> GetQueueMessageAsync(string serviceName)
        {
            var result = await GetDatabase().ListRightPopLeftPushAsync(
                $"{_redis_pre_key}:{serviceName}:mqrecieved",
                $"{_redis_pre_key}:{serviceName}:mqinprogress"
            );
            return (string?)result ?? "";
        }

        public Task<string> GetQueueMessageAsync() => GetQueueMessageAsync(ServiceName ?? "");

        public async Task<string> MarkQueueMessageAsync(string jsonUMFMessage, bool completed)
        {
            IReceivedUMF? umfHeader = DeserializeReceviedUMF(jsonUMFMessage);
            if (umfHeader != null && _redis != null)
            {
                UMFRouteEntry entry = umfHeader.GetRouteEntry();
                if (entry != null)
                {
                    EmitDebug(DebugEventType.MarkQueueMessage, jsonUMFMessage, $"Marking queue message");
                    var db = GetDatabase();
                    await db.ListRemoveAsync($"{_redis_pre_key}:{entry.ServiceName}:mqinprogress", jsonUMFMessage, -1);
                    if (completed == false)
                    {
                        // message was not completed, 
                        await db.ListRightPushAsync($"{_redis_pre_key}:{entry.ServiceName}:mqrecieved", jsonUMFMessage);
                    }
                }
            }
            return jsonUMFMessage;
        }

        bool _disposed = false;

        public async ValueTask ShutdownAsync(bool waitForflush = true, CancellationToken ct = default)
        {
            if (_disposed)
                return;
            try
            {
                try
                {
                    _cts.Cancel();
                    if (waitForflush)
                        await FlushMessageEvents(ct);
                }
                finally
                {
                    _cts.Dispose();
                }
                if (_redis != null)
                {
                    //attempt to remove InstanceID from hash
                    await GetDatabase().HashDeleteAsync(_nodes_hash_key, InstanceID);
                    await _redis.DisposeAsync();
                }
            }
            finally
            {
                _disposed = true;
            }
        }

        public void Shutdown() => ShutdownAsync().GetAwaiter().GetResult();

        public void Dispose() => Shutdown();

        public ValueTask DisposeAsync() => ShutdownAsync();

        /** *********************************
        * [[ INTERNAL AND PRIVATE MEMBERS ]]
        * ***********************************
        */

        async Task HandleMessage(RedisValue value)
        {
            if (_MessageHandler is null)
                return;
            try
            {
                string msg = (string?)value ?? String.Empty;
                EmitDebug(DebugEventType.MessageReceived, msg, $"Message received");
                var umf = ReceivedUMF.Deserialize(msg);
                var inMsg = new InboundMessage
                {
                    ReceivedUMF = umf,
                    Type = umf?.Typ ?? "",
                    MessageJson = msg,
                };
                await AddMessageChannelAction(Task.Run(async () =>
                {
                    //ensure responses are resolved after message handler completes
                    await _MessageHandler(inMsg);
                    if (inMsg.ReceivedUMF != null)
                        await _responseHandler.TryResolveResponses(inMsg);
                }));
            }
            catch (Exception e)
            {
                await EmitError(e);
            }
        }

        private ResponseHandler _responseHandler = new ResponseHandler();

        private async Task RegisterService()
        {
            if (_redis == null)
                return;
            EmitDebug(DebugEventType.Register, "", $"Registering to hydra");
            var db = GetDatabase();
            var key = $"{_redis_pre_key}:{ServiceName}:service";
            await db.StringSetAsync(key, StandardSerializer.SerializeBytes(new RegistrationEntry
            {
                ServiceName = ServiceName,
                Type = ServiceType,
            }));
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(_KEY_EXPIRATION_TTL));

            //use concurrent subscription
            await _redis.GetSubscriber().SubscribeAsync($"{_mc_message_key}:{ServiceName}", (c, m) => Task.Run(() => HandleMessage(m)));
            await _redis.GetSubscriber().SubscribeAsync($"{_mc_message_key}:{ServiceName}:{InstanceID}", (c, m) => Task.Run(() => HandleMessage(m)));
        }

        private TaskCompletionSource<bool> _initTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitInitialized() => _initTcs.Task;

        public IConnectionMultiplexer GetRedisConnection()
        {
            if (!IsInitialized)
                throw new HydraException("Hydra has not been initialized, so the connection is not available", HydraException.ErrorType.NotInitialized);
            if (_redis is null)
                throw new HydraException("The connection is unavailable");
            return _redis;
        }

        public async ValueTask<IConnectionMultiplexer> GetRedisConnectionAsync()
        {
            if (!IsInitialized)
                await WaitInitialized();
            return GetRedisConnection();
        }

        public IRedisConfig GetRedisConfig()
        {
            if (_config?.Redis is null)
                throw new NullReferenceException("Redis configuration is null, check your configuration");
            return _config.Redis;
        }

        public delegate Task InternalErrorHandler(Exception exception);
        private InternalErrorHandler? _ErrorHandler;

        public void OnInternalErrorHandler(InternalErrorHandler handler)
        {
            if (handler is null)
                throw new ArgumentNullException("handler", "InternalErrorHandler cannot be null");
            _ErrorHandler = handler;
        }

        async ValueTask EmitError(Exception e)
        {
            if (_ErrorHandler != null)
                await _ErrorHandler(e);
        }

        public delegate void InternalDebugHandler(DebugEvent debugEvent);
        private InternalDebugHandler? _DebugHandler;

        public void OnDebugEventHandler(InternalDebugHandler handler)
        {
            if (handler is null)
                throw new ArgumentNullException("handler", "InternalDebugHandler cannot be null");
            _DebugHandler = handler;
        }

        void EmitDebug(DebugEventType debugEventType, string umf, string message)
        {
            if (_DebugHandler is null || _config is null || !_config.EmitDebugEvents)
                return;
            if (_config.EmitDebugMaxUmfLength.GetValueOrDefault() > 0 && umf.Length > _config.EmitDebugMaxUmfLength)
                umf = umf.Substring(0, _config.EmitDebugMaxUmfLength.Value);
            _DebugHandler(new DebugEvent
            {
                EventType = debugEventType,
                UMF = umf,
                Message = message,
            });
        }
    }
}