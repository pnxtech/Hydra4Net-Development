using System.Text.Json;

namespace Hydra4NET.Helpers
{
    public class StandardSerializer
    {
        //caching these options improves performance
        private static readonly JsonSerializerOptions DeserializeOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions SerializeOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// A standardized JSON deserializer helper. This is essential as Hydra-based services written in non-Dotnet environments expect a universal format.
        /// </summary>
        /// <returns></returns>
        static public U? Deserialize<U>(string message) where U : class
        {
            return JsonSerializer.Deserialize<U>(message, DeserializeOptions);
        }

        /// <summary>
        /// A standardized JSON serializer helper which ensures that the generated JSON is compatible with JavaScript camel case. This is essential as Hydra-based services written in non-Dotnet environments expect a universal format.
        /// </summary>
        /// <returns></returns>
        static public string Serialize<T>(T item)
        {
            return JsonSerializer.Serialize(item, SerializeOptions);
        }

        /// <summary>
        /// A standardized JSON serializer helper which ensures that the generated JSON is compatible with JavaScript camel case. Serializes directly to UTF8 bytes for better performance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        static public byte[] SerializeBytes<T>(T item)
        {
            return JsonSerializer.SerializeToUtf8Bytes(item, SerializeOptions);
        }
    }
}
