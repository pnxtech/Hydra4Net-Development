using Hydra4NET;
using Microsoft.Extensions.Configuration;

namespace Hydra4Net.HostingExtensions
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Loads Hydra config from the specified IConfigurationSection
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static HydraConfigObject GetHydraConfig(this IConfigurationSection config) => config.Get<HydraConfigObject>();

        /// <summary>
        /// Loads Hydra config from the IConfigurationSection "Hydra"
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static HydraConfigObject GetHydraConfig(this IConfiguration config) => config.GetSection("Hydra").GetHydraConfig();
    }
}
