using System.Text.Json;

/**
 * Configuration
 * Hydra configuration loader
 */
namespace Hydra4NET
{
    static public class Configuration
    {
        static public HydraConfigObject? Load(string configPath)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            string json = File.ReadAllText(configPath);
            return (JsonSerializer.Deserialize<HydraConfigObject>(json, options));
        }
    }

    public class HydraConfigObject
    {
        public HydraRoot? Hydra { get; set; }
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

