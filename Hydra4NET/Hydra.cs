using System.Net;
using System.Net.Sockets;
using StackExchange.Redis;
using System.Text.Json;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

/**
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
        private const string _INFO = "info";
        private const string _DEBUG = "debug";
        private const string _WARN = "warn";
        private const string _ERROR = "error";
        private const string _FATAL = "fatal";
        private const string _TRACE = "trace";
        #endregion

        #region Class variables 
        private Task? _internalTask = null;
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
        public bool Initialized { get; private set; }

        private IServer? _server;
        private ConnectionMultiplexer? _redis;


        #endregion // Class variables

        HydraConfigObject _config;
        #region Message delegate
        public delegate Task UMFMessageHandler(UMF? umf, string type, string? message);
        private UMFMessageHandler? _MessageHandler = null;
        #endregion // Message delegate

        public Hydra(HydraConfigObject config = null)
        {
            LoadConfig(config);
        }

        void LoadConfig(HydraConfigObject config)
        {
            _config = config;
            ServiceName = _config?.Hydra?.ServiceName;
            ServiceDescription = _config?.Hydra?.ServiceDescription;
            ServiceType = _config?.Hydra?.ServiceType;
            ServicePort = _config?.Hydra?.ServicePort.ToString() ?? "";
            ServiceIP = _config?.Hydra?.ServiceIP;
        }

        #region Initialization

      

        public async Task Init(HydraConfigObject? config = null)
        {
            if (Initialized)
                throw new HydraInitException("This instance has already been initialized");
            if (config != null)
                LoadConfig(config);
            if(_config is null)
                throw new HydraInitException("No HydraConfigObject has been provided");
            try
            {
                //probably throw if no config passed or invalid?
                _internalTask = UpdatePresence(); // allows for calling UpdatePresence without awaiting
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
                //validate conn string here and give detailed errors if something missing?
                if (_redis != null && _redis.IsConnected)
                {
                    _server = _redis.GetServer($"{_config.Hydra.Redis.Host}:{_config.Hydra.Redis.Port}");
                    await RegisterService();
                    Initialized = true;
                }
                else
                {
                    throw new HydraInitException("Failed to initialize Hydra, connection to redis failed");
                }
            }
            catch (HydraInitException)
            {
                throw;
            }
            catch(Exception e)
            {
                throw new HydraInitException("Failed to initialize Hydra", e);
            }
            
        }
        #endregion


        public Task SendMessage(string to, string jsonUMFMessage)
            => SendMessage(UMFBase.ParseRoute(to), jsonUMFMessage);

        public Task SendMessage<T>(UMF<T> message) where T : new()
            => SendMessage(message.GetRouteEntry(), message.Serialize());

        private async Task SendMessage(UMFRouteEntry parsedEntry, string jsonUMFMessage)
        {
            string instanceId = string.Empty;
            if (parsedEntry.Instance != string.Empty)
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

        public Task SendBroadcastMessage<T>(UMF<T> message) where T : new()
            => SendBroadcastMessage(message.GetRouteEntry(), message.Serialize());

        public Task SendBroadcastMessage(string to, string jsonUMFMessage)
            => SendBroadcastMessage(UMFBase.ParseRoute(to), jsonUMFMessage);
        
        private async Task SendBroadcastMessage(UMFRouteEntry parsedEntry, string jsonUMFMessage)
        {
            if (_redis != null)
            {
                ISubscriber sub = _redis.GetSubscriber();
                await sub.PublishAsync($"{_mc_message_key}:{parsedEntry.ServiceName}", jsonUMFMessage);
            }
        }
  
        public void OnMessageHandler(UMFMessageHandler handler)
        {
            if (handler != null)
            {
                _MessageHandler = handler;
            }
        }
        
        private async Task QueueMessage(UMFRouteEntry? entry, string jsonUMFMessage)
        {
            if (entry.Error == string.Empty && _redis != null)
            {
                await _redis.GetDatabase().ListLeftPushAsync($"{_redis_pre_key}:{entry.ServiceName}:mqrecieved", jsonUMFMessage);
            }
        }

        public Task QueueMessage<T>(UMF<T> umfHeader) where T : new() =>
            QueueMessage(umfHeader?.GetRouteEntry(), umfHeader?.Serialize() ?? "");


        public Task QueueMessage(string jsonUMFMessage)
        {
            UMF? umfHeader = ExtractUMFHeader(jsonUMFMessage);
            return QueueMessage(umfHeader?.GetRouteEntry(), jsonUMFMessage);
        }

        //think about deserializing for them
        public async Task<string> GetQueueMessage(string serviceName)
        {
            string jsonUMFMessage = String.Empty;
            if (_redis != null)
            {
                var result = await _redis.GetDatabase().ListRightPopLeftPushAsync(
                    $"{_redis_pre_key}:{serviceName}:mqrecieved",
                    $"{_redis_pre_key}:{serviceName}:mqinprogress"
                );
                jsonUMFMessage = (string?)result ?? "";
            }
            return jsonUMFMessage;
        }

        public Task<string> GetQueueMessage() => GetQueueMessage(ServiceName ?? "");


        public async Task<string> MarkQueueMessage(string jsonUMFMessage, bool completed)
        {
            UMF? umfHeader = ExtractUMFHeader(jsonUMFMessage);
            if (umfHeader != null && _redis != null)
            {
                UMFRouteEntry entry = umfHeader.GetRouteEntry();
                if (entry != null)
                {
                    var db = _redis.GetDatabase();
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

        public void Shutdown()
        {
            try
            {
                _redis?.Dispose();
                _cts?.Cancel();
                _cts?.Dispose();
            }
            catch { }

        }
        public void Dispose()
        {
            Shutdown();
        }
        public async ValueTask DisposeAsync()
        {
            if(_redis != null)
            {
                await _redis.DisposeAsync();
                _redis = null;
            }
            Shutdown();
        }

        /** *********************************
        * [[ INTERNAL AND PRIVATE MEMBERS ]]
        * ***********************************
        */

        static UMF? ExtractUMFHeader(string jsonUMFString)
        {
            return JsonSerializer.Deserialize<UMF>(jsonUMFString, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });
        }
        
        async Task HandleMessage(ChannelMessage channelMessage)
        {
            string msg = (string?)channelMessage.Message ?? String.Empty;
            if (_MessageHandler != null)
            {
                var umf = UMF.Deserialize(msg);           
                await _MessageHandler(umf, umf?.Typ ?? "", msg);
            }
        }

        private async Task RegisterService()
        {
            if (_redis != null)
            {
                var db = _redis.GetDatabase();
                var key = $"{_redis_pre_key}:{ServiceName}:service";
                await db.StringSetAsync(key, Serialize(new RegistrationEntry
                {
                    ServiceName = ServiceName,
                    Type = ServiceType,
                    RegisteredOn = GetTimestamp()
                }));
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(_KEY_EXPIRATION_TTL));
            }

            if (_redis != null)
            {
                _redis.GetSubscriber().Subscribe($"{_mc_message_key}:{ServiceName}").OnMessage(HandleMessage);
                _redis.GetSubscriber().Subscribe($"{_mc_message_key}:{ServiceName}:{InstanceID}").OnMessage(HandleMessage);
            }
        }

      
    }


}

