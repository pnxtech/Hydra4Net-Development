using System.Text.Json;

namespace Hydra4NET
{
    public partial class Hydra
    {
        private string _Serialize(object message)
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
