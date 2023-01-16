using Hydra4NET.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

/**
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
            public string? RegisteredOn { get; set; }
        }

        public class MemoryStatsEntry
        {
            public long PagedMemorySize64 { get; set; }
            public long PeekPagedMemorySize64 { get; set; }
            public long VirtualPagedMemorySize64 { get; set; }
        }

        public class HealthCheckEntry
        {
            public string? UpdatedOn { get; set; }
            public string? ServiceName { get; set; }
            public string? InstanceID { get; set; }
            public string? HostName { get; set; }
            public string? SampledOn { get; set; }
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
            public string? UpdatedOn { get; set; }
        }
        #endregion // Entry classes

        #region Presence and Health check handling
        private string BuildHealthCheckEntry()
        {
            var timestamp = Iso8601.GetTimestamp();
            HealthCheckEntry healthCheckEntry = new HealthCheckEntry()
            {
                UpdatedOn = timestamp,
                ServiceName = ServiceName,
                InstanceID = InstanceID,
                HostName = HostName,
                SampledOn = timestamp,
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

            return StandardSerializer.Serialize(healthCheckEntry);
        }

        private string BuildPresenceNodeEntry()
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
                UpdatedOn = Iso8601.GetTimestamp()
            };
            return StandardSerializer.Serialize(presenceNodeEntry);
        }

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
                await db.HashSetAsync($"{_redis_pre_key}:nodes", InstanceID, BuildPresenceNodeEntry());
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

        public async Task<List<PresenceNodeEntry>> GetPresenceAsync(string serviceName)
        {
            List<string> instanceIds = new List<string>();
            List<PresenceNodeEntry> serviceEntries = new List<PresenceNodeEntry>();
            var server = GetServer();
            foreach (var key in server.Keys(pattern: $"*:{serviceName}:*:presence"))
            {
                string segments = key.ToString();
                var segmentParts = segments.Split(":");
                if (segmentParts.Length > 4)
                    instanceIds.Add(segmentParts[3]);
            }
            foreach (var id in instanceIds)
            {
                string? s = await GetDatabase().HashGetAsync($"{_redis_pre_key}:nodes", id);
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
            // Shuffle array using Fisher-Yates shuffle
            // Leverage tuples for a quick swap ;-)
            Random rng = new Random();
            int n = serviceEntries.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (serviceEntries[n], serviceEntries[k]) = (serviceEntries[k], serviceEntries[n]);
            }
            return serviceEntries;
        }

        #endregion // Presence and Health check handling
    }
}


