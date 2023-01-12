using System;

namespace Hydra4NET
{
    public class Iso8601
    {
        /// <summary>
        /// Retreive an ISO 8601 formatted UTC string from the current time.
        /// </summary>
        /// <returns></returns>
        public static string GetTimestamp() => GetTimeStamp(DateTime.Now);

        /// <summary>
        /// Retreive an ISO 8601 formatted UTC string from a DateTime
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static string GetTimeStamp(DateTime datetime) =>
            datetime.ToUniversalTime().ToString("u").Replace(" ", "T");
    }
}
