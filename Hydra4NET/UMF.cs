using Hydra4NET.Helpers;
using System;

/**
 * UMF - Universal Message Format
 * This module implements UMF based messages for standard interoperability 
 * between Hydra-based services, messaging and job queuing.
 * See: https://github.com/pnxtech/umf
 */
namespace Hydra4NET
{
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
        protected DateTime _Timestamp;

        public UMFBase()
        {
            _To ??= String.Empty;
            _From ??= String.Empty;
            _Mid = Guid.NewGuid().ToString();
            _Type = String.Empty;
            _Version = _UMF_Version;
            _Timestamp = DateTime.UtcNow;
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

        public string? Rmid { get; set; }

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

        public DateTime Ts
        {
            get { return _Timestamp; }
            set
            {
                //ensure serialization works as expected
                _Timestamp = value.ToUniversalTime();
            }
        }

        public virtual object? Bdy { get; set; }

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


    }

    /// <summary>
    /// The Generics-based UMF class that's used to implement a UMF and body message pair. T is the class type of the UMF's message body.
    /// </summary>
    /// <typeparam name="TBdy"></typeparam>
    public class UMF<TBdy> : UMFBase, IUMF<TBdy> where TBdy : new()
    {
        public new TBdy Bdy { get; set; } = new TBdy();

        public UMF() : base() { }

        /// <summary>
        /// Deserializes a UMF JSON message into a typed UMF class instance
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        static public UMF<TBdy>? Deserialize(string message) => StandardSerializer.Deserialize<UMF<TBdy>>(message);

        /// <summary>
        /// A JSON serializer helper which ensures that the generated JSON is compatible with JavaScript camel case. This is essential as Hydra-based services written in non-Dotnet environments expect a universal format.
        /// </summary>
        /// <returns></returns>
        public string Serialize() => StandardSerializer.Serialize(this);
    }
}

