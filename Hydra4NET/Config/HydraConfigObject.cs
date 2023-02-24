using Hydra4NET.Helpers;
using System;
using System.IO;

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
            return Redis.GetConnectionString();
        }

        /// <summary>
        /// Loads hydra config from the specified JSON file
        /// </summary>
        /// <param name="configJsonPath"></param>
        /// <returns></returns>
        static public HydraConfigObject? Load(string configJsonPath)
        {
            if (string.IsNullOrWhiteSpace(configJsonPath))
                throw new ArgumentNullException(nameof(configJsonPath), "Json path cannot be null or empty");
            if (!File.Exists(configJsonPath))
                throw new FileNotFoundException("Json path not found");
            return StandardSerializer.Deserialize<ConventionJsonWrapper>(File.ReadAllText(configJsonPath))?.Hydra;
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
