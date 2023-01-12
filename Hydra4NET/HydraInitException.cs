using System;

namespace Hydra4NET
{
    internal class HydraInitException : Exception
    {
        public HydraInitException(string message) : base(message) { }
        public HydraInitException(string message, Exception innerException) : base(message, innerException) { }
    }
}
