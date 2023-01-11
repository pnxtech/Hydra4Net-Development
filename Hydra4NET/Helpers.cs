using System;
using System.Text.Json;

/**
 * Hydra helper functions
 */
namespace Hydra4NET
{
    public partial class Hydra
    {
        /// <summary>
        /// A JSON serializer helper which ensures that the generated JSON is compatible with JavaScript camel case. This is essential as Hydra-based services written in non-Dotnet environments expect a universal format.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private string Serialize(object message)
        {
            return JsonSerializer.Serialize(message, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        /// <summary>
        /// Retreive an ISO 8601 formatted UTC string
        /// </summary>
        /// <returns></returns>
        public static string GetTimestamp()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
        }
    }

}

