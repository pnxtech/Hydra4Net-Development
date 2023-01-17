using System;

namespace Hydra4NET
{
    /// <summary>
    /// Used to hold parsed UMF route entries
    /// </summary>
    public class UMFRouteEntry
    {
        private string error = String.Empty;
        public string Instance { get; set; } = String.Empty;
        public string SubID { get; set; } = String.Empty;
        public string ServiceName { get; set; } = String.Empty;
        public string HttpMethod { get; set; } = String.Empty;
        public string ApiRoute { get; set; } = String.Empty;
        public string Error
        {
            get
            {
                return error;
            }
            set
            {
                Instance = String.Empty;
                SubID = String.Empty;
                ServiceName = String.Empty;
                HttpMethod = String.Empty;
                ApiRoute = String.Empty;
                error = value;
            }
        }
    }
}
