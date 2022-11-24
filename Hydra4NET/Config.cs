using ConfigJson;

namespace Hydra4NET
{
    public class Config
    {
        pubic Load(string configPath)
        {
            string json = File.ReadAllText(configPath);
            Rootobject config = JsonConvert.DeserializeObject<Rootobject>(json);
        }
    }
}
