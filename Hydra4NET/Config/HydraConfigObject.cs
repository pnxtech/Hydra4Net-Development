using System;
using System.IO;
using System.Text.Json;

namespace Hydra4NET
{
    /// <summary>
    /// Configuration object for hydra instance
    /// </summary>
    public class HydraConfigObject
    {
        public string? ServiceName { get; set; }
        public string? ServiceIP { get; set; }
        public int? ServicePort { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceDescription { get; set; }
        public Plugins? Plugins { get; set; }
        public RedisConfig? Redis { get; set; }

        public string GetRedisConnectionString()
        {
            if (Redis == null)
                throw new NullReferenceException("Redis configuration is null");
            //no default database in case the ConnectionMultiplexer is accessed outside hydra
            string connectionString = Redis.GetRedisHost();
            if (!string.IsNullOrWhiteSpace(Redis.Options))
            {
                connectionString = $"{connectionString},{Redis.Options}";
            }
            return connectionString;
        }

        /// <summary>
        /// Loads hydra config from the pecified JSON file
        /// </summary>
        /// <param name="configJsonPath"></param>
        /// <returns></returns>
        static public HydraConfigObject? Load(string configJsonPath)
        {
            if (string.IsNullOrWhiteSpace(configJsonPath))
                throw new ArgumentNullException(nameof(configJsonPath), "Json path cannot be null or empty");
            if (!File.Exists(configJsonPath))
                throw new FileNotFoundException("Json path not found");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<ConventionJsonWrapper>(File.ReadAllText(configJsonPath), options)?.Hydra;
        }
    }

    /// <summary>
    /// Wraps the conventional "hydra" object in json
    /// </summary>
    internal class ConventionJsonWrapper
    {
        public HydraConfigObject? Hydra { get; set; }
    }

    public class Plugins
    {
        public HydraLogger? HydraLogger { get; set; }
    }

    public class HydraLogger
    {
        public bool LogToConsole { get; set; }
        public bool OnlyLogLocally { get; set; }
    }

}
