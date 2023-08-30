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
            return $"{Host}:{Port ?? 6379}";
        }
        public string GetConnectionString()
        {
            //no default database in case the ConnectionMultiplexer is accessed outside hydra
            string connectionString = GetRedisHost();
            if (!string.IsNullOrWhiteSpace(Options))
            {
                connectionString = $"{connectionString},{Options}";
            }
            return connectionString;
        }
    }
}
