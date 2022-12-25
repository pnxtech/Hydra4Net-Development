using System.Text.Json;

/**
 * Hydra helper functions
 */
namespace Hydra4NET
{
    public partial class Hydra
    {
        private string Serialize(object message)
        {
            return JsonSerializer.Serialize(message, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public static string GetTimestamp()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
        }
    }
}
