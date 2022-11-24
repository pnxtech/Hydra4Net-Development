using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra4NET.ConfigJson
{
    public class Rootobject
    {
        public Hydra hydra { get; set; }
    }

    public class Hydra
    {
        public string serviceName { get; set; }
        public string serviceIP { get; set; }
        public int servicePort { get; set; }
        public string serviceType { get; set; }
        public string serviceDescription { get; set; }
        public Plugins plugins { get; set; }
        public Redis redis { get; set; }
    }

    public class Plugins
    {
        public Hydralogger hydraLogger { get; set; }
    }

    public class Hydralogger
    {
        public bool logToConsole { get; set; }
        public bool onlyLogLocally { get; set; }
    }

    public class Redis
    {
        public string urlxxx { get; set; }
        public string host { get; set; }
        public int port { get; set; }
        public int db { get; set; }
    }
}

