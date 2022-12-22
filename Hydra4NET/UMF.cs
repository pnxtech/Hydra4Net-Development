using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hydra4NET
{
    public class UMF
    {
        private const string _UMF_Version = "UMF/1.4.6";

        public string To { get; set; }
        public string From { get; set; }
        public string Mid { get; set; }
        public string Version { get; set; }
        public string Timestamp { get; set; }
        public object Body { get; set; }

        public UMF()
        {
            To ??= "";
            From ??= "";
            Mid = Guid.NewGuid().ToString();
            Version = _UMF_Version;
            Timestamp = UMF.GetTimestamp();
            Body = new object();
        }

        public static string GetTimestamp()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
        }

        public static string Serialize(object message)
        {
            return JsonSerializer.Serialize(message, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }    

    /*
    public class UMF
    {
        public bool Validate(UMFBaseMessage message)
        {
            return false;
        }

        public static string GetTimestamp()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
        }
    }
    */
}
