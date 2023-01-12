using System.Text.Json;

namespace Hydra4NET
{
    public class StandardSerializer
    {
        /// <summary>
        /// A standardized JSON deserializer helper. This is essential as Hydra-based services written in non-Dotnet environments expect a universal format.
        /// </summary>
        /// <returns></returns>
        static public U? Deserialize<U>(string message) where U : class
        {
            return JsonSerializer.Deserialize<U>(message, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// A standardized JSON serializer helper which ensures that the generated JSON is compatible with JavaScript camel case. This is essential as Hydra-based services written in non-Dotnet environments expect a universal format.
        /// </summary>
        /// <returns></returns>
        static public string Serialize<T>(T item)
        {
            return JsonSerializer.Serialize(item, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
