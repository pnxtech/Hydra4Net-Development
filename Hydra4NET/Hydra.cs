﻿using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public string? ServiceDescription { get; set; }
        public string? ServiceIP { get; set;}
        public string? ServicePort { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceVersion { get; set;}

        public string? InstanceID { get; set; }

        ConnectionMultiplexer? _redis;
        IDatabase? _db;

        private class _RegistrationEntry 
        { 
            public string? ServiceName { get; set; }
            public string? Type { get; set; }
            public string? RegisteredOn { get; set; }
        }

        public class _MemoryStats
        {
            public long PagedMemorySize64 { get; set; }
            public long PeekPagedMemorySize64 { get; set; }
            public long VirtualPagedMemorySize64 { get; set; }
        }

        private class _HealthCheckEntry
        {
            public string? UpdatedOn { get; set; }
            public string? ServiceName { get; set;} 
            public string? InstanceID { get; set; }
            public string? HostName { get; set; }
            public string? SampledOn { get; set; }
            public int ProcessID { get; set; }
            public string? Architecture { get; set; }
            public string? Platform { get; set; }
            public string? NodeVersion {
                get; set;
            }

            public _MemoryStats? Memory { get; set; }

            public double? UptimeSeconds { get; set; }
        }

        public Hydra()
        {
            TimeSpan interval = TimeSpan.FromSeconds(_ONE_SECOND);
            _timer = new PeriodicTimer(interval);
        }

        #region Initialization
        public async Task Init(HydraConfigObject config)
        {
            _internalTask = _UpdatePresence();
            ServiceName = config?.Hydra?.ServiceName;
            ServiceDescription = config?.Hydra?.ServiceDescription;
            ServiceType = config?.Hydra?.ServiceType;
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

            String connectionString = $"{config?.Hydra?.Redis?.Host}:{config?.Hydra?.Redis?.Port},defaultDatabase={config?.Hydra?.Redis?.Db}";
            if (config?.Hydra?.Redis?.Options != String.Empty)
            {
                connectionString = $"{connectionString},{config?.Hydra?.Redis?.Options}";
            }
            _redis = ConnectionMultiplexer.Connect(connectionString);
            if (_redis != null)
            {
                _db = _redis.GetDatabase();
                await _RegisterService();
            }
        }

        private async Task _RegisterService()
        {
            string jsonString = JsonSerializer.Serialize(new _RegistrationEntry
            {
                ServiceName = ServiceName,
                Type = ServiceType,
                RegisteredOn = UMF.GetTimestamp()
            }, new JsonSerializerOptions() { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            if (_db != null)
            {
                await _db.StringSetAsync($"{_redis_pre_key}:{ServiceName}:service", jsonString);
            }
        }
        #endregion

        #region Presence and Health check handling
        private string _BuildHealthCheckEntry()
        {
            _HealthCheckEntry healthCheckEntry = new()
            {
                UpdatedOn = UMF.GetTimestamp(),
                ServiceName = ServiceName,
                InstanceID = InstanceID,
                HostName = Dns.GetHostName(),
                SampledOn = UMF.GetTimestamp(),
                ProcessID = Environment.ProcessId,
                Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"),
                Platform = "Dotnet",
                NodeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
            };

            Process proc = Process.GetCurrentProcess();
            healthCheckEntry.Memory = new _MemoryStats
            {
                PagedMemorySize64 = proc.PagedMemorySize64,
                PeekPagedMemorySize64 = proc.PagedMemorySize64,
                VirtualPagedMemorySize64 = proc.VirtualMemorySize64
            };

            var runtime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            healthCheckEntry.UptimeSeconds = runtime.TotalSeconds;

            string jsonString = JsonSerializer.Serialize(healthCheckEntry, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return jsonString;
        }

        private async Task _UpdatePresence()
        {
            try 
            { 
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    await _PresenceEvent();
                    if (_secondsTick++ == _HEALTH_UPDATE_INTERVAL)
                    {
                        await _HealthCheckEvent();
                        _secondsTick = _ONE_SECOND;
                    }
                }
            }
            catch (OperationCanceledException) 
            {
            }
        }

        private async Task _PresenceEvent()
        {
            Console.WriteLine("Handling Update Presence");
            await Task.Delay(100);
        }

        private async Task _HealthCheckEvent()
        {
            if (_db != null)
            {
                await _db.StringSetAsync($"{_redis_pre_key}:{ServiceName}:{InstanceID}:health", _BuildHealthCheckEntry());
            }
            Console.WriteLine("Handling Update Health");
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
