using Hydra4NET;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Loads Hydra config from the IConfigurationSection "HydraConfig"
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static HydraConfigObject GetHydraConfig(this IConfiguration config) => config.GetSection("HydraConfig").GetHydraConfig();

    }
}
