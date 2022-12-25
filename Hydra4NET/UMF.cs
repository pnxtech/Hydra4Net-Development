using System.Text.Json;

namespace Hydra4NET
{
    /** 
     * UMFRouteEntry
     * Used to hold parsed UMF route entries
     */
    public class UMFRouteEntry
    {
        public string Instance { get; set; } = String.Empty;
        public string SubID { get; set; } = String.Empty;
        public string ServiceName { get; set; } = String.Empty;
        public string HttpMethod { get; set; } = String.Empty;
        public string ApiRoute { get; set; } = String.Empty;
        public string Error { get; set; } = String.Empty;
    }

    public class UMFBase
    {
        protected const string _UMF_Version = "UMF/1.4.6";
        protected string _To;
        protected string _From;
        protected string _Mid;
        protected string _Type;
        protected string _Version;
        protected string _Timestamp;

        protected UMFBase()
        {
            _To ??= String.Empty;
            _From ??= String.Empty;
            _Mid = Guid.NewGuid().ToString();
            _Type = String.Empty;
            _Version = _UMF_Version;
            _Timestamp = GetTimestamp();
        }

        public string To
        {
            get { return _To; }
            set { _To = value; }
        }

        public string Frm
        {
            get { return _From; }
            set { _From = value; }
        }

        public string Mid
        {
            get { return _Mid; }
            set { _Mid = value; }
        }

        public string Typ
        {
            get { return _Type; }
            set { _Type = value; }
        }

        public string Ver
        {
            get { return _Version; }
            set { _Version = value; }
        }

        public string Ts
        {
            get { return _Timestamp; }
            set { _Timestamp = value; }
        }

        public static string GetTimestamp()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
        }

        /**
         * FlushRouteEntry
         * Blank out Parse Route Entry except for Error field
         */
        public static UMFRouteEntry FlushRouteEntry(UMFRouteEntry entry)
        {
            entry.Instance = String.Empty;
            entry.SubID = String.Empty;
            entry.ServiceName = String.Empty;
            entry.HttpMethod = String.Empty;
            entry.ApiRoute = String.Empty;        
            return entry;
        }

        /**
         * ParseRoute
         * Parses a string based UMF route into individual route entries
         * Sample entries:
         *      "hydra-router:/"
         *      "hydra-router:[post]/"
         *      "de571e9695c24c0eb12834ae5ee2f404@hydra-router:/"
         *      "de571e9695c24c0eb12834ae5ee2f404-8u0f9wls7r@hydra-router:[get]/"
         */
        public static UMFRouteEntry ParseRoute(string route)
        {
            UMFRouteEntry routeEntry = new UMFRouteEntry();
            if (route == String.Empty)
            {
                routeEntry.Error = "route is empty";
                return routeEntry;
            }
            var segments = route.Split(":");
            if (segments.Length < 2)
            {
                routeEntry.Error = "route field has invalid number of routable segments";
            } else
            {
                var subSegments = segments[0].Split("@");
                if (subSegments.Length == 1)
                {
                    routeEntry.ServiceName = segments[0];
                }
                else
                {
                    if (subSegments[0].IndexOf("-") > -1)
                    {
                        var subID = subSegments[0].Split("-");
                        var l = subID.Length;
                        if (l == 1)
                        {
                            routeEntry.ServiceName = subID[0];
                        }
                        else if (l > 1)
                        {
                            routeEntry.Instance = subID[0];
                            routeEntry.SubID = subID[1];
                            routeEntry.ServiceName = subSegments[1];
                        }
                    }
                    else
                    {
                        routeEntry.Instance = subSegments[0];
                        routeEntry.ServiceName = subSegments[1];
                    }
                }
                var lb = segments[1].IndexOf("[");
                var rb = segments[1].IndexOf("]");
                if (lb > -1 && rb > -1)
                {
                    routeEntry.HttpMethod = segments[1].Substring(lb + 1, rb - 1);
                    segments[1] = segments[1].Substring(rb + 1);
                }
                else
                {
                    routeEntry = FlushRouteEntry(routeEntry);
                    routeEntry.Error = "route has mismatched http [ or ] brackets";
                }
                routeEntry.ApiRoute = segments[1];
            }
            return routeEntry;
        }
    }

    public class UMF<T> : UMFBase where T : class, new()
    {
        private T _Body;

        public T Bdy 
        { 
            get { return _Body; } 
            set { _Body = value; }  
        }

        public UMF()
        {
            _Body = new T();
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        static public U? Deserialize<U>(string message)
        {
            return JsonSerializer.Deserialize<U>(message, new JsonSerializerOptions() 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }
    }    
}
