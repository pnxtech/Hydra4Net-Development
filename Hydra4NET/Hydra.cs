using System.Net;
using System.Net.Sockets;
using StackExchange.Redis;
using System.Text.Json;

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

namespace Hydra4NET;

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

    //the docs say IDatabase does not need to be stored.  id rather not store it in case it is not thread-safe 
    //private IDatabase? _db;
    #endregion // Class variables

    // Storing it as a property  allows easier DI IMO.  we should probably pick either contructor or init though
    HydraConfigObject _config;
    #region Message delegate
    public delegate Task MessageHandler(string type, string? message); 
    public delegate Task UMFHandler(UMF? umf, string type, string? message);
    private MessageHandler? _MessageHandler = null;
    private UMFHandler? _UmfHandler = null;
    #endregion // Message delegate

    public Hydra(HydraConfigObject config = null)
    {
        TimeSpan interval = TimeSpan.FromSeconds(_ONE_SECOND);
        _timer = new PeriodicTimer(interval);
        _config = config;
    }

    #region Initialization
    public async Task Init(HydraConfigObject config = null)
    {
        if (config != null)
            _config = config;
        //probably throw if no config passed or invalid??
        _internalTask = UpdatePresence(); // allows for calling UpdatePresence without awaiting
        HostName = Dns.GetHostName();
        ProcessID = Environment.ProcessId;
        Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        NodeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        ServiceName = _config?.Hydra?.ServiceName;
        ServiceDescription = _config?.Hydra?.ServiceDescription;
        ServiceType = _config?.Hydra?.ServiceType;
        ServicePort = _config?.Hydra?.ServicePort.ToString() ?? "";
        ServiceIP = _config?.Hydra?.ServiceIP;
        if (ServiceIP == null || ServiceIP == String.Empty)
        {
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
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

        InstanceID = Guid.NewGuid().ToString();
        InstanceID = InstanceID.Replace("-", "");

        //wed probably want some kind of log output instead of directly to console.  We could have an inbuild console option (eg via the plugin) or custom log sinks (eg asp core's ILogger)
        Console.WriteLine($"{ServiceName} ({InstanceID}) listening on {ServiceIP}");
        string connectionString = $"{_config?.Hydra?.Redis?.Host}:{_config?.Hydra?.Redis?.Port},defaultDatabase={_config?.Hydra?.Redis?.Db}";
        if (_config?.Hydra?.Redis?.Options != String.Empty)
        {
            connectionString = $"{connectionString},{_config?.Hydra?.Redis?.Options}";
        }
        _redis = ConnectionMultiplexer.Connect(connectionString);
        if (_redis != null && _redis.IsConnected)
        {
            _server = _redis.GetServer($"{_config?.Hydra?.Redis?.Host}:{_config?.Hydra?.Redis?.Port}");
            //_db = _redis.GetDatabase();
            await RegisterService();
        }
        else
        {
            throw new HydraInitException("Failed to initialize Hydra, connection to redis failed");
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
    public Task SendMessage(string to, string jsonUMFMessage)
        => SendMessage(UMFBase.ParseRoute(to), jsonUMFMessage);

    public Task SendMessage<T>(UMF<T> message) where T :  new()
        => SendMessage(message.GetRouteEntry(), message.Serialize());

    private async Task SendMessage(UMFRouteEntry parsedEntry, string jsonUMFMessage)
    {
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
    //this is just an idea for better typing
    public void OnMessageHandler(UMFHandler handler)
    {
        if (handler != null)
        {
            _UmfHandler = handler;
        }
    }

    public async Task QueueMessage(UMFRouteEntry? entry, string jsonUMFMessage)
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

    public async Task<String> GetQueueMessage(string serviceName)
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

    /**
     * MarkQueueMessage
     * Message is popped off the "in progress" queue and if the completed
     * flag is set to false then the message is requeued on the the
     * "mqrecieved" queue.
     * 
     * Note at this time this function does not support a reason code 
     * for requeuing.
     */
    public async Task<String> MarkQueueMessage(string jsonUMFMessage, bool completed)
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
            if (_internalTask != null)
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                    //_internalTask;
                    _cts.Dispose();
                }          
            }
            _redis?.Dispose();
            _timer?.Dispose();
        }
        catch { }
      
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
    static string GetType(string jsonUMFString)
    {
        UMF? baseUMF = ExtractUMFHeader(jsonUMFString);
        return (baseUMF != null) ? baseUMF.Typ : "";
    }

    async Task HandleMessage(ChannelMessage channelMessage)
    {
        string msg = (string?)channelMessage.Message ?? String.Empty;
        var type = GetType(msg);
        //we should probably pick one or the other
        if (_MessageHandler != null)
        {
            await _MessageHandler(type, msg);
        }
        if (_UmfHandler != null)
        {
            var umf = UMF.Deserialize(msg);
            await _UmfHandler(umf, type, msg);
        }
    }

    private async Task RegisterService()
    {
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync($"{_redis_pre_key}:{ServiceName}:service", Serialize(new RegistrationEntry
            {
                ServiceName = ServiceName,
                Type = ServiceType,
                RegisteredOn = GetTimestamp()
            }));
            await db.KeyExpireAsync($"{_redis_pre_key}:{ServiceName}:service", TimeSpan.FromSeconds(_KEY_EXPIRATION_TTL));
        }

        if (_redis != null)
        {
            ISubscriber subChannel1 = _redis.GetSubscriber();
            ISubscriber subChannel2 = _redis.GetSubscriber();
            subChannel1.Subscribe($"{_mc_message_key}:{ServiceName}").OnMessage(HandleMessage);
            subChannel2.Subscribe($"{_mc_message_key}:{ServiceName}:{InstanceID}").OnMessage(HandleMessage);
        }
    }
}

