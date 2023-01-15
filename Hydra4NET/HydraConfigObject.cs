using System.IO;
using System.Text.Json;

/**
 * Configuration
 * Hydra configuration loader
 */
namespace Hydra4NET
{

    public class HydraConfigObject
    {
        public HydraRoot? Hydra { get; set; }
        public string GetRedisConnectionString()
        {
            var redis = Hydra?.Redis;
            string connectionString = $"{redis?.Host}:{redis?.Port},defaultDatabase={redis?.Db}";
            if (redis?.Options != string.Empty)
            {
                connectionString = $"{connectionString},{redis?.Options}";
            }
            return connectionString;
        }

        /// <summary>
        /// Loads hydra config from the pecified JSON file
        /// </summary>
        /// <param name="configPath"></param>
        /// <returns></returns>
        static public HydraConfigObject? Load(string configJsonPath)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            string json = File.ReadAllText(configJsonPath);
            return (JsonSerializer.Deserialize<HydraConfigObject>(json, options));
        }

    }

    public class HydraRoot
    {
        public string? ServiceName { get; set; }
        public string? ServiceIP { get; set; }
        public int? ServicePort { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceDescription { get; set; }
        public Plugins? Plugins { get; set; }
        public Redis? Redis { get; set; }
    }

    public class Plugins
    {
        public Hydralogger? HydraLogger { get; set; }
    }

    public class Hydralogger
    {
        public bool LogToConsole { get; set; }
        public bool OnlyLogLocally { get; set; }
    }

    public class Redis
    {
        public string? Host { get; set; }
        public int Port { get; set; }
        public int Db { get; set; }
        public string? Options { get; set; }
    }
}
