using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra4NET.ConfigJson
{
    public class HydraConfigObject
    {
        public Hydra? Hydra { get; set; }
    }

    public class Hydra
    {
        public string? ServiceName { get; set; }
        public string? ServiceIP { get; set; }
        public int? ServicePort { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceDescription { get; set; }
        public Plugins? Plugins { get; set; }
        public Redis? Redis { get; set; }
    }

    public class Plugins
    {
        public Hydralogger? HydraLogger { get; set; }
    }

    public class Hydralogger
    {
        public bool LogToConsole { get; set; }
        public bool OnlyLogLocally { get; set; }
    }

    public class Redis
    {
        public string? Urlxxx { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public int Db { get; set; }
    }
}

