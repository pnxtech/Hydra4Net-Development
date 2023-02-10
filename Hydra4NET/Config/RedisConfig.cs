using System;

namespace Hydra4NET
{
    public class RedisConfig : IRedisConfig
    {
        public string? Host { get; set; }
        public int? Port { get; set; }
        public int Db { get; set; }
        public string? Options { get; set; }
        public string GetRedisHost()
        {
            if (string.IsNullOrWhiteSpace(Host))
                throw new ArgumentNullException(nameof(Host), "Host cannot be null or empty");
            var host = Host;
            if (Port.HasValue)
                host += $":{Port}";
            return host;
        }
    }
}
