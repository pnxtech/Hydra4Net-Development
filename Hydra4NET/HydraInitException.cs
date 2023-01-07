using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydra4NET
{
    internal class HydraInitException : Exception
    {
        public HydraInitException(string message) : base(message) { }
        public HydraInitException(string message, Exception innerException) : base(message, innerException) { }
    }
}
