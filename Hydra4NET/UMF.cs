using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/**
 * UMF - Universal Message Format
 * This module implements UMF based messages for standard interoperability 
 * between Hydra-based services, messaging and job queuing.
 * See: https://github.com/pnxtech/umf
 */
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

    /**
     * UMFBase
     * UMF base class used by the UMF class
     */
    public abstract class UMFBase
    {
        protected const string _UMF_Version = "UMF/1.4.6";
        protected string _To;
        protected string _From;
        protected string _Mid;
        protected string _Type;
        protected string _Version;
        protected string _Timestamp;

        public UMFBase()
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

        public virtual object Bdy { get; set; }

        //maybe handle like this in case they want to serialize manually?
        //public void SetMessageJson(string json) => _MessageJson = json;
        //public string GetMessageJson() => _MessageJson;

        [JsonIgnore] //prevent System.Text.Json from serializing / deserializing
        public string MessageJson { get; set; }


        /// <summary>
        /// Retreive an ISO 8601 formatted UTC string
        /// </summary>
        /// <returns></returns>
        public static string GetTimestamp()
        {
            DateTime dateTime = DateTime.Now;
            return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
        }


        /// <summary>
        ///Parses a string based UMF route into individual route entries.
        ///Sample entries:
        ///"hydra-router:/"
        ///"hydra-router:[post]/"
        ///"de571e9695c24c0eb12834ae5ee2f404@hydra-router:/"
        ///"de571e9695c24c0eb12834ae5ee2f404-8u0f9wls7r@hydra-router:[get]/"
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
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
            }
            else
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
                    routeEntry.ApiRoute = segments[1];
                }
                else if (lb == -1 && rb == -1)
                {
                    routeEntry.ApiRoute = segments[1];
                }
                else
                {
                    routeEntry.Error = "route has mismatched http [ or ] brackets";
                }
            }
            return routeEntry;
        }
        /// <summary>
        /// Gets a UMFRouteEntry from the instance's To field
        /// </summary>
        /// <returns></returns>
        public UMFRouteEntry GetRouteEntry() => ParseRoute(To);


        /**
         * Deserilize
         * JSON Deserilization helper
         **/
        static public U? Deserialize<U>(string message) where U : UMFBase
        {
            var umf = JsonSerializer.Deserialize<U>(message, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });
            if (umf != null)
                umf.MessageJson = message;
            return umf;
        }

    }

    /// <summary>
    /// The Generics-based UMF class that's used to implement a UMF and body message pair. T is the class type of the UMF's message body.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UMF<T> : UMFBase where T : new()
    {
        public new T Bdy { get; set; } = new T();
        public UMF() : base()
        {
            
        }
        /// <summary>
        /// Deserializes a UMF JSON message into a typed UMF class instance
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        static public UMF<T>? Deserialize(string message) => Deserialize<UMF<T>>(message);

        //doesnt work in base class
        /// <summary>
        /// A JSON serializer helper which ensures that the generated JSON is compatible with JavaScript camel case. This is essential as Hydra-based services written in non-Dotnet environments expect a universal format.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

    }
    //Consider making UMF<string> and forcing serialization of the Bdy as a raw json string
    /// <summary>
    /// The UMF class that's used to implement an untyped UMF and body message pair.
    /// </summary>
    public class UMF : UMF<JsonElement>
    {
        public UMF() : base() { }
        /// <summary>
        /// Deserializes a UMF JSON message into an untyped UMF class instance
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        static public new UMF? Deserialize(string message) => Deserialize<UMF>(message);
        /// <summary>
        /// Casts an untyped UMF instance to a typed instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UMF<T> Cast<T>() where T : new() => UMF<T>.Deserialize(MessageJson)!;
        /// <summary>
        /// Casts an untyped UMF instance into a derived class of typed UMF
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public U Cast<U, T>() where U : UMF<T> where T : new() => Deserialize<U>(MessageJson)!;

    }
}

