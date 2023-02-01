using System.IO;
using System.Text.Json;

namespace Hydra4NET
{
    //TODO: We can probably make "hydraRoot" the top level, instead of nesting it
    /// <summary>
    /// Configuration object for hydra instance
    /// </summary>
    public class HydraConfigObject
    {
        public HydraRoot? Hydra { get; set; }
        public string GetRedisConnectionString()
        {
            var redis = Hydra?.Redis;
            //no default database in case the ConnectionMultiplexer is accessed outside hydra
            string connectionString = $"{redis?.Host}:{redis?.Port}";
            if (redis?.Options != string.Empty)
            {
                connectionString = $"{connectionString},{redis?.Options}";
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
