using System.Diagnostics;

namespace Hydra4NET
{
    public partial class Hydra
    {
        #region Entry classses
        private class RegistrationEntry
        {
            public string? ServiceName { get; set; }
            public string? Type { get; set; }
            public string? RegisteredOn { get; set; }
        }

        private class MemoryStatsEntry
        {
            public long PagedMemorySize64 { get; set; }
            public long PeekPagedMemorySize64 { get; set; }
            public long VirtualPagedMemorySize64 { get; set; }
        }

        private class HealthCheckEntry
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

        private class PresenceNodeEntry
        {
            public string? ServiceName { get; set; }
            public string? ServiceDescription { get; set; }
            public string? Version { get; set; }
            public string? InstanceID { get; set; }
            public int ProcessID { get; set; }
            public string? Ip { get; set; }
            public int Port { get; set; }
            public string? HostName { get; set; }
            public string? UpdatedOn { get; set; }
        }
        #endregion // Entry classes

        #region Presence and Health check handling
        private string BuildHealthCheckEntry()
        {
            HealthCheckEntry healthCheckEntry = new()
            {
                UpdatedOn = GetTimestamp(),
                ServiceName = ServiceName,
                InstanceID = InstanceID,
                HostName = HostName,
                SampledOn = GetTimestamp(),
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

            return _Serialize(healthCheckEntry);
        }

        private string BuildPresenceNodeEntry()
        {
            PresenceNodeEntry presenceNodeEntry = new()
            {
                ServiceName = ServiceName,
                ServiceDescription = ServiceDescription,
                Version = "",
                InstanceID = InstanceID,
                ProcessID = ProcessID,
                Ip = ServiceIP,
                Port = ServicePort,
                HostName = HostName,
                UpdatedOn = GetTimestamp()
            };
            return _Serialize(presenceNodeEntry);
        }

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
            if (_db != null)
            {
                await _db.StringSetAsync($"{_redis_pre_key}:{ServiceName}:{InstanceID}:presence", InstanceID);
                await _db.KeyExpireAsync($"{_redis_pre_key}:{ServiceName}:{InstanceID}:presence", TimeSpan.FromSeconds(_KEY_EXPIRATION_TTL));
                await _db.HashSetAsync($"{_redis_pre_key}:nodes", InstanceID, BuildPresenceNodeEntry());
            }
        }

        private async Task HealthCheckEvent()
        {
            if (_db != null)
            {
                await _db.StringSetAsync($"{_redis_pre_key}:{ServiceName}:{InstanceID}:health", BuildHealthCheckEntry());
                await _db.KeyExpireAsync($"{_redis_pre_key}:{ServiceName}:{InstanceID}:health", TimeSpan.FromSeconds(_KEY_EXPIRATION_TTL));
            }
        }
        #endregion // Presence and Health check handling
    }
}
