using System.Text.Json;
using Hydra4NET.ConfigJson;

namespace Hydra4NET
{
    static public class Config
    {
        static public HydraConfigObject? Load(string configPath)
        {
            string json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<HydraConfigObject>(json);
        }
    }
}
