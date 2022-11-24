using Hydra4NET.ConfigJson;

namespace Hydra4NET
{
    public class Config
    {
        public void Load(string configPath)
        {
            string json = File.ReadAllText(configPath);
            Rootobject config = JsonConvert.DeserializeObject<Rootobject>(json);
        }
    }
}
