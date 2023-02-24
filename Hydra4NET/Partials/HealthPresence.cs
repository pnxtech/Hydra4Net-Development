using Hydra4NET.Helpers;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

/*
 * Hydra Health and Presence module
 * This module implements a periodic timer which retrieves operating 
 * system process stats and metrics and periodically writes them to Redis.
 */
namespace Hydra4NET
{
    public partial class Hydra
    {
        #region Entry classses
        public class RegistrationEntry
        {
            public string? ServiceName { get; set; }
            public string? Type { get; set; }
            public DateTime RegisteredOn { get; set; } = DateTime.UtcNow;
        }

        public class MemoryStatsEntry
        {
            public long PagedMemorySize64 { get; set; }
            public long PeekPagedMemorySize64 { get; set; }
            public long VirtualPagedMemorySize64 { get; set; }
        }

        public class HealthCheckEntry
        {
            public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
            public string? ServiceName { get; set; }
            public string? InstanceID { get; set; }
            public string? HostName { get; set; }
            public DateTime SampledOn { get; set; } = DateTime.UtcNow;
            public int ProcessID { get; set; }
            public string? Architecture { get; set; }
            public string? Platform { get; set; }
            public string? NodeVersion
            {
                get; set;
            }
            public MemoryStatsEntry? Memory { get; set; }
            public double? UptimeSeconds { get; set; }
        }

        public class PresenceNodeEntry
        {
            public string? ServiceName { get; set; }
            public string? ServiceDescription { get; set; }
            public string? Version { get; set; }
            public string? InstanceID { get; set; }
            public int ProcessID { get; set; }
            public string? Ip { get; set; }
            public string? Port { get; set; }
            public string? HostName { get; set; }
            public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
            public int Elapsed { get; set; }
        }
        #endregion // Entry classes

        #region Presence and Health check handling
        private byte[] BuildHealthCheckEntry()
        {
            HealthCheckEntry healthCheckEntry = new HealthCheckEntry()
            {
                ServiceName = ServiceName,
                InstanceID = InstanceID,
                HostName = HostName,
                ProcessID = ProcessID,
                Architecture = Architecture,
                Platform = "Dotnet",
                NodeVersion = NodeVersion
            };

            Process proc = Process.GetCurrentProcess();
            healthCheckEntry.Memory = new MemoryStatsEntry
            {
                PagedMemorySize64 = proc.PagedMemorySize64,
                PeekPagedMemorySize64 = proc.PagedMemorySize64,
                VirtualPagedMemorySize64 = proc.VirtualMemorySize64
            };

            var runtime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            healthCheckEntry.UptimeSeconds = runtime.TotalSeconds;

            return StandardSerializer.SerializeBytes(healthCheckEntry);
        }

        private byte[] BuildPresenceNodeEntry()
        {
            PresenceNodeEntry presenceNodeEntry = new PresenceNodeEntry()
            {
                ServiceName = ServiceName,
                ServiceDescription = ServiceDescription,
                Version = "",
                InstanceID = InstanceID,
                ProcessID = ProcessID,
                Ip = ServiceIP,
                Port = ServicePort,
                HostName = HostName,
                Elapsed = 0
            };
            return StandardSerializer.SerializeBytes(presenceNodeEntry);
        }

        private void ConfigurePresenceTask() => _presenceTask = UpdatePresence(); // allows for calling UpdatePresence without awaiting

        private async Task UpdatePresence()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await PresenceEvent();
                    if (_secondsTick++ == _HEALTH_UPDATE_INTERVAL)
                    {
                        await HealthCheckEvent();
                        _secondsTick = _ONE_SECOND;
                    }
                    await Task.Delay(_ONE_SECOND * 1000, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task PresenceEvent()
        {
            if (_redis != null)
            {
                var db = GetDatabase();
                await db.StringSetAsync($"{_redis_pre_key}:{ServiceName}:{InstanceID}:presence", InstanceID);
                await db.KeyExpireAsync($"{_redis_pre_key}:{ServiceName}:{InstanceID}:presence", TimeSpan.FromSeconds(_KEY_EXPIRATION_TTL));
                await db.HashSetAsync(_nodes_hash_key, InstanceID, BuildPresenceNodeEntry());
            }
        }

        private async Task HealthCheckEvent()
        {
            if (_redis != null)
            {
                var db = GetDatabase();
                await db.StringSetAsync($"{_redis_pre_key}:{ServiceName}:{InstanceID}:health", BuildHealthCheckEntry());
                await db.KeyExpireAsync($"{_redis_pre_key}:{ServiceName}:{InstanceID}:health", TimeSpan.FromSeconds(_KEY_EXPIRATION_TTL));
            }
        }

        public async Task<PresenceNodeEntryCollection> GetPresenceAsync(string serviceName)
        {
            List<string> instanceIds = new List<string>();
            PresenceNodeEntryCollection serviceEntries = new PresenceNodeEntryCollection();
            var server = GetServer();
            await foreach (var key in server.KeysAsync(pattern: $"*:{serviceName}:*:presence"))
            {
                string segments = key.ToString();
                var segmentParts = segments.Split(":");
                if (segmentParts.Length > 4)
                    instanceIds.Add(segmentParts[3]);
            }
            foreach (var id in instanceIds)
            {
                string? s = await GetDatabase().HashGetAsync(_nodes_hash_key, id);
                if (s != null)
                {
                    PresenceNodeEntry? presenceNodeEntry = JsonSerializer.Deserialize<PresenceNodeEntry>(s, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    if (presenceNodeEntry != null)
                    {
                        serviceEntries.Add(presenceNodeEntry);
                    }
                }
            }

            return serviceEntries;
        }

        static readonly DateTime _time1970 = new DateTime(1970, 1, 1);
        private int GetUtcTimeStamp(DateTime dateRef) => (int)(dateRef.ToUniversalTime().Subtract(_time1970)).TotalSeconds;

        public async Task<PresenceNodeEntryCollection> GetServiceNodesAsync()
        {
            var timeNow = GetUtcTimeStamp(DateTime.Now);
            PresenceNodeEntryCollection serviceEntries = new PresenceNodeEntryCollection();
            var db = GetDatabase();
            HashEntry[] list = await db.HashGetAllAsync($"{_redis_pre_key}:nodes");
            foreach (var entry in list)
            {
                PresenceNodeEntry? presenceNodeEntry = JsonSerializer.Deserialize<PresenceNodeEntry>(entry.Value, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                if (presenceNodeEntry != null)
                {
                    var unixTimestamp = GetUtcTimeStamp(presenceNodeEntry.UpdatedOn);
                    presenceNodeEntry.Elapsed = timeNow - unixTimestamp;
                    serviceEntries.Add(presenceNodeEntry);
                }
            }
            return serviceEntries;
        }

        #endregion // Presence and Health check handling
    }
}
