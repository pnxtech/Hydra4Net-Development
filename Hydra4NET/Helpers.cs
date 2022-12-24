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
    }
}
